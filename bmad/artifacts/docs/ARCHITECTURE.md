# Singleton Notepad — Architecture

**Status:** v1 · **Owner:** Architect · **Date:** 2026-04-25
**Input:** `bmad/artifacts/docs/PRD.md`

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────┐
│  Avalonia 11 (Cross-platform Desktop)                   │
│                                                         │
│  ┌──────────┐   ┌──────────────────────────────────┐   │
│  │ Program  │   │  Views (AXAML + ViewModels)       │   │
│  │ .cs      │──▶│  MainView / SettingsView          │   │
│  │ App boot │   │  Pages: Apparence/Fichiers        │   │
│  │ mutex    │   └──────────┬───────────────────────┘   │
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

### Program.cs — Entry Point
- Standard Avalonia app builder pattern
- Configures Fluent theme and platform detection
- Starts with `ClassicDesktopStyleApplicationLifetime`

### App.axaml.cs — Bootstrap & DI
- Registers all services in the DI container (`Microsoft.Extensions.DependencyInjection`)
- Enforces single-instance via named mutex
- On second launch: brings existing window to front and exits

### MainWindow.axaml.cs — Window Lifecycle
- On `Opened`: reads `ISettingsService` for saved X/Y/W/H, validates position is on primary monitor
- On `Closing`: persists current bounds
- `MonitorHelper` encapsulates position validation — isolated, testable

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

- UI thread: all Avalonia controls, ViewModel property changes (via `Avalonia.Threading.Dispatcher`).
- Background: file I/O (async/await), LLM HTTP call (async/await), `FileSystemWatcher` callbacks.
- Rule: services never touch `Dispatcher` — ViewModels marshal results back via `Dispatcher.UIThread.Post`.

---

## 5. File / Folder Structure

```
SingletonNotepad/
├── SingletonNotepad.sln
├── SingletonNotepad/                       # main project (Avalonia)
│   ├── Program.cs                          # Entry point
│   ├── App.axaml(.cs)
│   ├── MainWindow.axaml(.cs)
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
│   │       └── MonitorHelper.cs
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainView.axaml(.cs)
│   │   ├── SettingsView.axaml(.cs)
│   │   ├── ApparenceSettingsPage.axaml(.cs)
│   │   └── FichiersSettingsPage.axaml(.cs)
│   └── Resources/
│       └── DefaultRules.md
└── SingletonNotepad.Tests/                 # MSTest
    ├── FileServiceTests.cs
    ├── SettingsServiceTests.cs
    ├── NormalizationServiceTests.cs        # mocked ILlmProvider
    └── MemoryTrackerServiceTests.cs
```

---

## 6. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| DI container | `Microsoft.Extensions.DependencyInjection` | Standard, no extra dep, cross-platform |
| Single-instance | Named mutex | Simple, works across all platforms |
| LLM abstraction | Strategy via `ILlmProvider` | Swap providers without touching NormalizationService |
| API key storage | Encrypted JSON file | Cross-platform, secure enough for local apps |
| Diff library | DiffPlex | Lightweight, line + word diff |
| Rate limit | In-service hash check (not UI) | Prevents duplicate normalize on identical content |
| Memory writes | MemoryTrackerService, called by ViewModel | Services stay independent; ViewModel controls the write timing |
| UI framework | Avalonia 11 | Cross-platform (Windows, Linux, macOS), stable, Fluent theme |

---

## 7. Test Architecture

- **Unit tests** (MSTest, no UI): `FileService`, `SettingsService`, `NormalizationService` (mock `ILlmProvider`), `MemoryTrackerService`.
- **Integration**: file I/O against temp paths (`Path.GetTempPath()`).
- No mocking of `FileService` in normalization tests — use temp files. Prefer real I/O over mock I/O at the boundary.

---

## 8. Sprint 1 Story Notes (for Dev)

| Story | Architecture note |
|-------|------------------|
| S1-01 | Wire DI in `App.axaml.cs`; register all interfaces. Use mutex for single-instance. |
| S1-02 | `FileService` exposes `LoadAsync`, `SaveAsync`, `WatchAsync`(returns `IDisposable`). Debounce in service, not ViewModel. |
| S1-03 | `SettingsService` typed: `Get<T>(key, defaultValue)` / `Set<T>(key, value)`. JSON-based storage. |
| S1-04 | `MonitorHelper.IsOnPrimaryMonitor(x, y)` → pure static, easily unit-testable with mock coords. |
| S1-05 | `Program.cs` creates mutex in `Main()` before `BuildAvaloniaApp()`. |
| S1-06 | `MainViewModel.SyncState` enum: `Saved / Unsaved / Saving`. StatusBar binds to this. |
| S1-07 | `SettingsView` uses `TabControl` with tabs for sub-pages (Apparence, Fichiers). |
| S1-08 | Tests use `[TestInitialize]` to create temp files; `[TestCleanup]` removes them. |
