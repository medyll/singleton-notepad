# Singleton Notepad — Architecture

**Status:** v1 · **Owner:** Architect · **Date:** 2026-04-25
**Input:** `bmad/artifacts/docs/PRD.md`

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────┐
│  WinUI 3 (Packaged / MSIX)                              │
│                                                         │
│  ┌──────────┐   ┌──────────────────────────────────┐   │
│  │ App.xaml │   │  Views (XAML + ViewModels)        │   │
│  │ .cs      │──▶│  MainView / SettingsView          │   │
│  │ DI boot  │   │  Controls: MarkdownEditor,        │   │
│  │ mutex    │   │            InlineDiffEditor        │   │
│  └──────────┘   └──────────┬───────────────────────┘   │
│                            │ binds                      │
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
│          │  OllamaProvider                          │    │
│          │  OpenAiProvider                          │    │
│          │  AnthropicProvider                       │    │
│          └──────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
         │ file I/O                      │ HTTP
         ▼                               ▼
   MY_SINGLETON_NOTEPAD.md        LLM endpoint
   NOTEPAD_SINGLETON_AGENTS.md    (Ollama / OpenAI / Anthropic)
   NOTEPAD_SINGLETON_MEMORY.md
```

---

## 2. Component Breakdown

### App.xaml.cs — Bootstrap & Guard
- Registers all services in the DI container (`Microsoft.Extensions.DependencyInjection`).
- Enforces single-instance via `AppInstance.FindOrRegisterForKey` (Win App SDK); falls back to named mutex as a defense layer.
- On second launch: activates the existing window and exits.

> Tradeoff: `AppInstance` is the idiomatic Win App SDK approach, but it only works within the same package identity. The mutex backup handles edge cases (debug vs release identity mismatch).

### MainWindow.xaml.cs — Window Lifecycle
- On `Activated`: reads `ISettingsService` for saved X/Y/W/H, validates position is on primary monitor, applies via `SetWindowPos` (P/Invoke). Falls back to `CenterOnPrimaryMonitor`.
- On `Closed`: persists current bounds, triggers optional auto-normalize.
- `MonitorHelper` encapsulates `MonitorFromWindow` / `GetMonitorInfo` P/Invokes — isolated, testable.

### IFileService / FileService
- Single responsibility: read, write, watch one file.
- Auto-creates file with empty template if absent.
- Auto-save: debounced 2s via `System.Threading.Timer` (reset on each keystroke).
- `FileSystemWatcher` raised on background thread → marshals to dispatcher for UI reload prompt.
- Retry: `FileIOException` → 3 attempts, exponential backoff (200ms / 400ms / 800ms).

### ISettingsService / SettingsService
- Thin wrapper over `ApplicationData.Current.LocalSettings`.
- Typed get/set via generic helper (avoids scattered casting).
- API keys: stored via `Windows.Security.Credentials.PasswordVault` (DPAPI-backed). `SettingsService` exposes `GetApiKey(provider)` / `SetApiKey(provider, key)`.

### INormalizationService / NormalizationService
- Orchestrates: load rules file → build prompt → call `ILlmProvider` → compute diff (DiffPlex) → return `NormalizationResult`.
- Rate-limit guard: blocks re-trigger if last normalize < 5 min AND file content unchanged (SHA-256 hash comparison).
- File size check: warns before sending if > 500 KB or > 10k lines (toast via `INotificationService`).
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
  → InlineDiffEditor renders diff
  → User: [Apply] or [Cancel]
  → Apply: FileService.WriteAsync() + MemoryTrackerService.AppendAsync()
```

### Settings Persist
```
SettingsViewModel property change
  → SettingsService.Set<T>()
  → LocalSettings (sync, no await needed)
```

---

## 4. Threading Model

- UI thread: all WinUI controls, ViewModel property changes (via `DispatcherQueue`).
- Background: file I/O (async/await), LLM HTTP call (async/await), `FileSystemWatcher` callbacks.
- Rule: services never touch `DispatcherQueue` — ViewModels marshal results back via `DispatcherQueue.TryEnqueue`.

---

## 5. File / Folder Structure

```
SingletonNotepad/
├── SingletonNotepad.sln
├── SingletonNotepad/                       # main project (WinUI 3 Packaged)
│   ├── Package.appxmanifest
│   ├── App.xaml(.cs)
│   ├── MainWindow.xaml(.cs)
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
│   │   │   └── INotificationService.cs     # toast abstraction
│   │   ├── Providers/
│   │   │   ├── ILlmProvider.cs
│   │   │   ├── OllamaProvider.cs
│   │   │   ├── OpenAiProvider.cs
│   │   │   └── AnthropicProvider.cs
│   │   ├── Models/
│   │   │   ├── AppSettings.cs
│   │   │   ├── NormalizationResult.cs      # includes DiffPlex result
│   │   │   ├── ChangeRecord.cs
│   │   │   └── LlmProviderConfig.cs
│   │   └── Helpers/
│   │       ├── MonitorHelper.cs            # P/Invoke isolation
│   │       ├── PathHelper.cs
│   │       └── DpapiHelper.cs             # PasswordVault wrapper
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainView.xaml(.cs)
│   │   ├── SettingsView.xaml(.cs)
│   │   └── Controls/
│   │       ├── MarkdownEditor.xaml(.cs)
│   │       └── InlineDiffEditor.xaml(.cs)
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
| DI container | `Microsoft.Extensions.DependencyInjection` | Standard, no extra dep, Win App SDK compatible |
| Single-instance | `AppInstance.FindOrRegisterForKey` + mutex fallback | Idiomatic SDK + resilience |
| LLM abstraction | Strategy via `ILlmProvider` | Swap providers without touching NormalizationService |
| API key storage | `PasswordVault` (DPAPI) | PRD requirement; more secure than plain LocalSettings |
| Diff library | DiffPlex | Already in spec; lightweight, line + word diff |
| Rate limit | In-service hash check (not UI) | Prevents duplicate normalize on identical content |
| Memory writes | MemoryTrackerService, called by ViewModel | Services stay independent; ViewModel controls the write timing |
| InlineDiffEditor | Custom `RichTextBlock`-based control | No suitable WinUI 3 off-the-shelf diff control |

---

## 7. Test Architecture

- **Unit tests** (MSTest, no UI): `FileService`, `SettingsService`, `NormalizationService` (mock `ILlmProvider`), `MemoryTrackerService`.
- **Integration**: file I/O against temp paths (`Path.GetTempPath()`).
- **UI / E2E**: Playwright for Sprint 3 — smoke test normalize flow on a real running instance.
- No mocking of `FileService` in normalization tests — use temp files. Prefer real I/O over mock I/O at the boundary.

---

## 8. Sprint 1 Story Notes (for Dev)

| Story | Architecture note |
|-------|------------------|
| S1-01 | Wire DI in `App.xaml.cs`; register all interfaces. Use `AppInstance` for single-instance. |
| S1-02 | `FileService` exposes `LoadAsync`, `SaveAsync`, `WatchAsync`(returns `IDisposable`). Debounce in service, not ViewModel. |
| S1-03 | `SettingsService` typed: `Get<T>(key, defaultValue)` / `Set<T>(key, value)`. |
| S1-04 | `MonitorHelper.IsOnPrimaryMonitor(x, y)` → pure static, easily unit-testable with mock coords. |
| S1-05 | `App.xaml.cs` calls `AppInstance.FindOrRegisterForKey("singleton-notepad")` in constructor before `InitializeComponent`. |
| S1-06 | `MainViewModel.SyncState` enum: `Saved / Unsaved / Saving`. StatusBar binds to this. |
| S1-07 | `SettingsView` uses `NavigationView` with frame-based navigation to sub-pages (Apparence, Fichiers) — not a single XAML page. |
| S1-08 | Tests use `[TestInitialize]` to create temp files; `[TestCleanup]` removes them. |
