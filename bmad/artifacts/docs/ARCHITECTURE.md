# Singleton Notepad — Architecture

**Status:** v2 · **Owner:** Architect · **Date:** 2026-05-05
**Input:** `bmad/artifacts/docs/PRD.md`

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────┐
│  WinUI 3 — Windows App SDK 2.0.1 (Packaged MSIX)        │
│  .NET 10 — net10.0-windows10.0.26100.0                  │
│                                                         │
│  ┌──────────┐   ┌──────────────────────────────────┐   │
│  │ App.xaml │   │  Views (XAML + ViewModels)        │   │
│  │ .cs      │──▶│  MainPage / SettingsPage          │   │
│  │ DI boot  │   │  Controls: MarkdownEditor         │   │
│  │ Mutex    │   │           InlineDiffEditor        │   │
│  └──────────┘   └──────────┬───────────────────────┘   │
│               ┌────────────▼───────────────────────┐   │
│               │  Core / Services (interfaces)       │   │
│               │  IFileService                       │   │
│               │  ISettingsService                   │   │
│               │  INormalizationService              │   │
│               │  IMemoryTrackerService              │   │
│               └────────────┬───────────────────────┘   │
│                            │ uses                       │
│          ┌─────────────────▼──────────────────────┐    │
│          │  Core / Providers                        │    │
│          │  ILlmProvider                            │    │
│          │  OllamaProvider (Sprint 2)               │    │
│          │  OpenAiProvider (Sprint 3)               │    │
│          │  AnthropicProvider (Sprint 3)            │    │
│          └──────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
         │ file I/O                       │ HTTP
         ▼                                ▼
   MY_SINGLETON_NOTEPAD.md         LLM endpoint
   NOTEPAD_SINGLETON_AGENTS.md     (Ollama / OpenAI / Anthropic)
   NOTEPAD_SINGLETON_MEMORY.md
```

---

## 2. Component Breakdown

### App.xaml.cs — Bootstrap & DI

- Registers all services via `Microsoft.Extensions.DependencyInjection`
- Single-instance via **named Mutex** `"SingletonNotepad-7B3F9A2C-Instance"`
- On second launch: P/Invoke `FindWindow` + `SetForegroundWindow` to bring existing window to front
- **NOT** `AppInstance.FindOrRegisterForKey` — that API requires activation event plumbing and is unreliable for simple single-instance enforcement
- Exposes `static App Current` and `IServiceProvider Services` for DI resolution from code-behind

```csharp
// Single-instance pattern
bool created;
var mutex = new Mutex(true, "SingletonNotepad-7B3F9A2C-Instance", out created);
if (!created) { BringExistingWindowToFront(); Application.Current.Exit(); return; }
```

### MainWindow.xaml.cs — Window Lifecycle

- On `Activated` / constructor: reads `ISettingsService` for saved X/Y/W/H
- Validates position via `MonitorHelper.IsWindowValidOnPrimaryMonitor()` (>50% visible)
- Falls back to `MonitorHelper.CenterOnPrimaryMonitor()` if invalid or first run
- Positions via `AppWindow.Move()` / `AppWindow.Resize()`
- On `Closed`: persists current bounds via `AppWindow.Position` / `AppWindow.Size`

### IFileService / FileService

- Single responsibility: read, write, watch one file.
- Auto-creates file with empty content if absent.
- Auto-save: debounced 2s via `System.Timers.Timer` (reset on each keystroke via `QueueAutoSave()`).
- `FileSystemWatcher` raised on background thread → marshal to dispatcher for UI reload prompt.
- Retry: `IOException` → 3 attempts, exponential backoff (200ms / 400ms / 800ms).

### ISettingsService / SettingsService

- JSON file: `%LocalAppData%\SingletonNotepad\settings.json`
- **Not** `ApplicationData.Current.LocalSettings` — JSON file gives full control and testability
- **Not** `PasswordVault` — API keys stored in plain JSON for Sprint 1; DPAPI encryption is Sprint 3
- `AppSettings` model: `NotesFilePath`, `Theme`, `AutoSave`, `AutoSaveDelayMs`, `WindowGeometry` (X/Y/W/H), `LlmProvider` settings

### INormalizationService / NormalizationService

- Orchestrates: load rules file → build prompt → call `ILlmProvider` → compute diff (DiffPlex) → return `NormalizationResult`.
- Rate-limit guard: blocks re-trigger if last normalize < 5 min AND file content unchanged (SHA-256 hash comparison).
- File size check: warns before sending if > 500 KB or > 10k lines.
- Never writes to file directly — returns result; ViewModel decides apply/cancel.

### IMemoryTrackerService / MemoryTrackerService

- Appends Markdown records to `NOTEPAD_SINGLETON_MEMORY.md`.
- Called by ViewModel after apply (not by NormalizationService — keeps concerns separate).

### ILlmProvider (Strategy pattern)

```csharp
public interface ILlmProvider
{
    string Name { get; }
    Task<string> CompleteAsync(string prompt, CancellationToken ct);
}
```

- Registered by name in DI; `NormalizationService` resolves the active provider from settings at call time.
- HTTP timeout: 30s `CancellationToken`-based.

### ViewModels (CommunityToolkit.Mvvm)

- `MainViewModel`: editor content, sync state, normalize command, diff preview state.
  Uses `[ObservableProperty]` source generator. Must store `DispatcherQueue` in constructor for thread-safe FileSaved handler.
- `SettingsViewModel`: all settings panes, provider switching, path pickers.
- Commands are `AsyncRelayCommand` — never block the UI thread.

### MonitorHelper

- Uses `DisplayArea.Primary.WorkArea` (returns `RectInt32`) — **not** `System.Windows.Forms.Screen`
- `IsWindowValidOnPrimaryMonitor(x, y, w, h)`: checks >50% of window overlaps primary work area
- `CenterOnPrimaryMonitor(w, h)`: returns `(x, y)` centered in primary work area
- `EnsureValidWindowPosition(x, y, w, h)`: combines above, returns safe position

---

## 3. Data Flow

### Normal Edit & Save

```
User types
  → TextBox.TextChanged
  → MainViewModel.OnContentChanged (partial method from [ObservableProperty])
  → FileService.QueueAutoSave()  [resets 2s timer]
  → 2s idle → FileService.SaveAsync()
  → FileSaved event → ViewModel marshals to DispatcherQueue
  → StatusBar shows "Sync ✓"
```

### Normalize (manual)

```
User clicks [Normaliser]
  → MainViewModel.NormalizeCommand
  → NormalizationService.NormalizeAsync()
      → load AGENTS.md (FileService)
      → rate-limit / size check
      → ILlmProvider.CompleteAsync()   [background Task]
  → NormalizationResult returned
  → Diff preview rendered (InlineDiffEditor)
  → User: [Appliquer] or [Annuler]
  → Apply: FileService.SaveAsync() + MemoryTrackerService.AppendAsync()
```

### Settings Persist

```
SettingsViewModel property change
  → SettingsService.Save(appSettings)
  → JSON file (async write)
```

---

## 4. Threading Model

- UI thread: all WinUI 3 controls, ViewModel property changes via `DispatcherQueue`.
- Background: file I/O (async/await), LLM HTTP call (async/await), `FileSystemWatcher` callbacks, `System.Timers.Timer` callbacks.
- Rule: services never touch `DispatcherQueue` — ViewModels marshal results back via `_dispatcherQueue.TryEnqueue()`.

---

## 5. File / Folder Structure

```
SingletonNotepad/
├── SingletonNotepad.slnx               ← .slnx with explicit x64/x86/ARM64 platforms
└── SingletonNotepad/                   ← main project (WinUI 3, net10.0-windows10.0.26100.0)
    ├── SingletonNotepad.csproj
    ├── App.xaml(.cs)                   ← DI bootstrap, named Mutex single-instance
    ├── MainWindow.xaml(.cs)            ← AppWindow positioning, DispatcherQueue
    ├── MainPage.xaml(.cs)              ← Editor shell, MenuBar, CommandBar, StatusBar
    ├── Package.appxmanifest
    ├── app.manifest
    ├── Assets/
    ├── Core/
    │   ├── Services/
    │   │   ├── IFileService.cs
    │   │   ├── FileService.cs
    │   │   ├── INormalizationService.cs
    │   │   ├── NormalizationService.cs
    │   │   ├── IMemoryTrackerService.cs
    │   │   ├── MemoryTrackerService.cs
    │   │   ├── ISettingsService.cs
    │   │   └── SettingsService.cs
    │   ├── Providers/
    │   │   ├── ILlmProvider.cs
    │   │   ├── OllamaProvider.cs       (Sprint 2)
    │   │   ├── OpenAiProvider.cs       (Sprint 3)
    │   │   └── AnthropicProvider.cs    (Sprint 3)
    │   ├── Models/
    │   │   ├── AppSettings.cs
    │   │   ├── NormalizationRule.cs
    │   │   └── ChangeRecord.cs
    │   └── Helpers/
    │       ├── MonitorHelper.cs        ← DisplayArea.Primary.WorkArea API
    │       └── PathHelper.cs
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   └── SettingsViewModel.cs
    ├── Views/
    │   ├── SettingsPage.xaml(.cs)
    │   └── Controls/
    │       ├── MarkdownEditor.xaml(.cs)
    │       └── InlineDiffEditor.xaml(.cs)
    └── Resources/
        └── DefaultRules.md
```

---

## 6. Build

> ⚠️ Toujours cibler le `.csproj` directement — la `.slnx` ne propage pas `-p:Platform`.

```bash
# Depuis SingletonNotepad/SingletonNotepad/
dotnet build SingletonNotepad.csproj -p:Platform=x64
dotnet test ..\SingletonNotepad.Tests\SingletonNotepad.Tests.csproj -p:Platform=x64
```

Même info dans : `README.md` · `SingletonNotepad.csproj` · `SingletonNotepad.slnx` · `bmad/status.md`

---

## 7. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| DI container | `Microsoft.Extensions.DependencyInjection` | Standard, no extra dep |
| Single-instance | Named Mutex + P/Invoke FindWindow/SetForegroundWindow | Simple, reliable; AppInstance redirect has activation plumbing overhead |
| Monitor detection | `DisplayArea.Primary.WorkArea` (WinUI3 API) | Native API; System.Windows.Forms.Screen would require extra dependency |
| Settings storage | JSON in `%LocalAppData%\SingletonNotepad\settings.json` | Full control, testable; LocalSettings is packaged-app scoped and harder to inspect |
| API key storage | Plain JSON (Sprint 1), DPAPI encryption (Sprint 3) | Avoid complexity in Sprint 1; PasswordVault adds packaging constraints |
| LLM abstraction | Strategy via `ILlmProvider` | Swap providers without touching NormalizationService |
| Diff library | DiffPlex | Lightweight, line + word diff |
| Memory writes | MemoryTrackerService, called by ViewModel | Services stay independent; ViewModel controls the write timing |
| UI framework | WinUI 3 — Windows App SDK 2.0.1 | Native Win11 Fluent; decision confirmed, not Avalonia |
| Auto-save timer | `System.Timers.Timer` (not `System.Threading.Timer`) | Consistent callback, easier to dispose cleanly |

---

## 7. Test Architecture

- **Unit tests** (MSTest, no UI): `FileService`, `SettingsService`, `NormalizationService` (mock `ILlmProvider`), `MonitorHelper`.
- **Integration**: file I/O against temp paths (`Path.GetTempPath()`).
- No mocking of `FileService` in normalization tests — use temp files. Prefer real I/O over mock I/O at the boundary.
- Test project TFM: `net10.0-windows10.0.26100.0` — must match main project.

---

## 8. Sprint 1 Story Notes (for Dev)

| Story | Architecture note |
|-------|------------------|
| S1-01 | Wire DI in `App.xaml.cs`. Named Mutex for single-instance. Rename `FreshRef` → `SingletonNotepad` in all files immediately after scaffold. |
| S1-02 | `FileService` exposes `LoadAsync`, `SaveAsync`, `Watch`, `QueueAutoSave`, `CancelAutoSave`. Debounce in service via `System.Timers.Timer`, not ViewModel. |
| S1-03 | `SettingsService`: JSON file at `%LocalAppData%\SingletonNotepad\settings.json`. `System.Text.Json` serialization. No LocalSettings, no PasswordVault. |
| S1-04 | `MonitorHelper` uses `DisplayArea.Primary.WorkArea` (returns `RectInt32`). No WinForms dependency. |
| S1-05 | Single-instance in `App.xaml.cs` `OnLaunched`. Mutex check before creating window. P/Invoke `FindWindow` by window class/title to bring front. |
| S1-06 | `MainPage` hosts MenuBar + CommandBar + `MarkdownEditor` UserControl + StatusBar. ViewModel as typed public property (required for x:Bind). |
| S1-07 | `SettingsPage` uses `NavigationView` (Left, `IsSettingsVisible=False`). ViewModel as typed public property. Navigate via `Frame.Navigate(typeof(SettingsPage))`. |
| S1-08 | Tests use `[TestInitialize]` for temp files; `[TestCleanup]` removes them. TFM: `net10.0-windows10.0.26100.0`. |
