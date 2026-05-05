# Singleton Notepad — Architecture

**Status:** v1 · **Owner:** Architect · **Date:** 2026-04-25
**Input:** `bmad/artifacts/docs/PRD.md`

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────┐
│  WinUI 3 — Windows App SDK 1.7 (Packaged MSIX)          │
│                                                         │
│  ┌──────────┐   ┌──────────────────────────────────┐   │
│  │ App.xaml │   │  Views (XAML + ViewModels)        │   │
│  │ .cs      │──▶│  MainPage / SettingsPage          │   │
│  │ DI boot  │   │  Pages: Apparence/Fichiers        │   │
│  │ AppInst  │   └──────────┬───────────────────────┘   │
│  └──────────┘              │ binds                      │
│               ┌────────────▼───────────────────────┐   │
│               │  Core / Services (interfaces)       │   │
│               │  IFileService                       │   │
│               │  ISettingsService                   │   │
│               │  INormalizationService              │   │
│               │  IMemoryTrackerService              │   │
│               │  INotificationService               │   │
│               └────────────┬───────────────────────┘   │
│                            │ uses                       │
│          ┌─────────────────▼──────────────────────┐    │
│          │  Core / Providers                        │    │
│          │  ILlmProvider                            │    │
│          │  OllamaProvider                          │    │
│          │  OpenAiProvider                          │    │
│          │  AnthropicProvider                       │    │
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
- Single-instance via `AppInstance.FindOrRegisterForKey("SingletonNotepad")`
- On second launch: activates existing window via `AppInstance.RedirectActivationTo()`

### MainWindow.xaml.cs — Window Lifecycle
- On `Activated`: reads `ISettingsService` for saved X/Y/W/H, positions via `AppWindow.Move()` / `AppWindow.Resize()`
- On `Closed`: persists current bounds via `AppWindow.Position` / `AppWindow.Size`
- `MonitorHelper` uses `DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary)` — isolated, testable

### IFileService / FileService
- Single responsibility: read, write, watch one file.
- Auto-creates file with empty template if absent.
- Auto-save: debounced 2s via `System.Threading.Timer` (reset on each keystroke).
- `FileSystemWatcher` raised on background thread → marshals to dispatcher for UI reload prompt.
- Retry: `FileIOException` → 3 attempts, exponential backoff (200ms / 400ms / 800ms).

### ISettingsService / SettingsService
- Thin wrapper over JSON file storage in AppData folder.
- Typed get/set via generic helper (avoids scattered casting).
- API keys: stored in separate encrypted JSON file. `SettingsService` exposes `GetApiKey(provider)` / `SetApiKey(provider, key)`.

### INormalizationService / NormalizationService
- Orchestrates: load rules file → build prompt → call `ILlmProvider` → compute diff (DiffPlex) → return `NormalizationResult`.
- Rate-limit guard: blocks re-trigger if last normalize < 5 min AND file content unchanged (SHA-256 hash comparison).
- File size check: warns before sending if > 500 KB or > 10k lines (notification via `INotificationService`).
- Never writes to file directly — returns result; ViewModel decides apply/cancel.

### IMemoryTrackerService / MemoryTrackerService
- Appends Markdown records to `NOTEPAD_SINGLETON_MEMORY.md`.
- Called by ViewModel after apply (not by NormalizationService — keeps concerns separate).
- Writes pre-normalization backup to `NOTEPAD_SINGLETON_MEMORY_backup_{timestamp}.md` in the same folder.

### ILlmProvider (Strategy pattern)
```csharp
public interface ILlmProvider
{
    string Name { get; }
    Task<string> NormalizeAsync(string rules, string content, CancellationToken ct);
}
```
- Registered by name in DI; `NormalizationService` resolves the active provider from settings at call time.
- `OllamaProvider` (Sprint 2), `OpenAiProvider` / `AnthropicProvider` (Sprint 3).
- HTTP timeout: 30s `CancellationToken`-based.

### ViewModels (CommunityToolkit.Mvvm)
- `MainViewModel`: editor content, sync state, normalize command, diff preview state.
- `SettingsViewModel`: all settings panes, provider switching, path pickers.
- Commands are `AsyncRelayCommand` — never block the UI thread.

---

## 3. Data Flow

### Normal Edit & Save
```
User types
  → TextBox.TextChanged
  → MainViewModel debounce timer resets
  → 2s idle → FileService.SaveAsync()
  → StatusBar shows "Sync ✓"
```

### Normalize (manual)
```
User clicks [Normalize]
  → MainViewModel.NormalizeCommand
  → NormalizationService.NormalizeAsync()
      → load AGENTS.md (FileService)
      → rate-limit / size check
      → ILlmProvider.NormalizeAsync()   [background Task]
  → NormalizationResult returned
  → Diff preview rendered
  → User: [Apply] or [Cancel]
  → Apply: FileService.WriteAsync() + MemoryTrackerService.AppendAsync()
```

### Settings Persist
```
SettingsViewModel property change
  → SettingsService.Set<T>()
  → JSON file (async write)
```

---

## 4. Threading Model

- UI thread: all WinUI 3 controls, ViewModel property changes via `DispatcherQueue`.
- Background: file I/O (async/await), LLM HTTP call (async/await), `FileSystemWatcher` callbacks.
- Rule: services never touch `DispatcherQueue` — ViewModels marshal results back via `_dispatcherQueue.TryEnqueue()`.

---

## 5. File / Folder Structure

```
SingletonNotepad/
├── SingletonNotepad.sln
├── SingletonNotepad/                       # main project (WinUI 3)
│   ├── SingletonNotepad.csproj
│   ├── App.xaml(.cs)                       # DI bootstrap, AppInstance single-instance
│   ├── MainWindow.xaml(.cs)               # AppWindow positioning, DispatcherQueue
│   ├── Package.appxmanifest
│   ├── app.manifest
│   ├── Assets/
│   ├── Core/
│   │   ├── Services/
│   │   │   ├── IFileService.cs
│   │   │   ├── FileService.cs
│   │   │   ├── INormalizationService.cs
│   │   │   ├── NormalizationService.cs
│   │   │   ├── IMemoryTrackerService.cs
│   │   │   ├── MemoryTrackerService.cs
│   │   │   ├── ISettingsService.cs
│   │   │   ├── SettingsService.cs
│   │   │   └── INotificationService.cs
│   │   ├── Providers/
│   │   │   ├── ILlmProvider.cs
│   │   │   ├── OllamaProvider.cs
│   │   │   ├── OpenAiProvider.cs
│   │   │   └── AnthropicProvider.cs
│   │   ├── Models/
│   │   │   ├── AppSettings.cs
│   │   │   ├── NormalizationResult.cs
│   │   │   ├── ChangeRecord.cs
│   │   │   └── LlmProviderConfig.cs
│   │   └── Helpers/
│   │       └── MonitorHelper.cs           # DisplayArea API
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainPage.xaml(.cs)
│   │   ├── SettingsPage.xaml(.cs)
│   │   └── Controls/
│   │       ├── MarkdownEditor.xaml(.cs)
│   │       └── InlineDiffEditor.xaml(.cs)
│   └── Resources/
│       └── DefaultRules.md
└── SingletonNotepad.Tests/                 # MSTest (net8.0-windows, UseWinUI=true)
    ├── SingletonNotepad.Tests.csproj
    ├── FileServiceTests.cs
    ├── SettingsServiceTests.cs
    ├── NormalizationServiceTests.cs        # mocked ILlmProvider
    └── MemoryTrackerServiceTests.cs
```

---

## 6. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| DI container | `Microsoft.Extensions.DependencyInjection` | Standard, no extra dep |
| Single-instance | `AppInstance.FindOrRegisterForKey()` | WinUI 3 native; handles activation redirect cleanly |
| LLM abstraction | Strategy via `ILlmProvider` | Swap providers without touching NormalizationService |
| API key storage | `PasswordVault` (Windows Credential Locker) | Secure, per-user, native Win32 |
| Diff library | DiffPlex | Lightweight, line + word diff |
| Rate limit | In-service hash check (not UI) | Prevents duplicate normalize on identical content |
| Memory writes | MemoryTrackerService, called by ViewModel | Services stay independent; ViewModel controls the write timing |
| UI framework | WinUI 3 — Windows App SDK 1.7 | Native Win11 Fluent, no cross-platform overhead |
| Settings storage | `ApplicationData.Current.LocalSettings` | Built-in, packaged-app scoped, no JSON needed for primitives |

---

## 7. Test Architecture

- **Unit tests** (MSTest, no UI): `FileService`, `SettingsService`, `NormalizationService` (mock `ILlmProvider`), `MemoryTrackerService`.
- **Integration**: file I/O against temp paths (`Path.GetTempPath()`).
- No mocking of `FileService` in normalization tests — use temp files. Prefer real I/O over mock I/O at the boundary.

---

## 8. Sprint 1 Story Notes (for Dev)

| Story | Architecture note |
|-------|------------------|
| S1-01 | Wire DI in `App.xaml.cs`; register all interfaces. `AppInstance.FindOrRegisterForKey("SingletonNotepad")` for single-instance. |
| S1-02 | `FileService` exposes `LoadAsync`, `SaveAsync`, `WatchAsync`(returns `IDisposable`). Debounce in service, not ViewModel. |
| S1-03 | `SettingsService`: `ApplicationData.Current.LocalSettings` for primitives; `PasswordVault` for API keys. |
| S1-04 | `MonitorHelper.GetPrimaryDisplayArea(windowId)` → wraps `DisplayArea.GetFromWindowId()`. Pure logic, unit-testable. |
| S1-05 | `AppInstance` redirect handled in `App.xaml.cs` `OnActivated`. No separate Program.cs needed. |
| S1-06 | `MainViewModel.SyncState` enum: `Saved / Unsaved / Saving`. StatusBar binds to this via `DispatcherQueue`. |
| S1-07 | `SettingsPage` uses `NavigationView` (Left mode, compact) with `Apparence` and `Fichiers` items. |
| S1-08 | Tests use `[TestInitialize]` to create temp files; `[TestCleanup]` removes them. Test project: `net8.0-windows10.0.19041.0`, `UseWinUI=true`. |
