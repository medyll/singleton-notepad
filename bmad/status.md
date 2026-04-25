# Singleton Notepad — Project Status

**Generated:** 2026-04-25  
**Phase:** development (35% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Active Story:** S1-02 — FileService + auto-create + auto-save (2s debounce)  
**Last Completed:** S1-01 — Scaffold WinUI 3 project + DI + MVVM wiring ✅

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | 🔄 pending | ⏳ |
| S1-03 | SettingsService over LocalSettings (paths, window geometry) | ⏳ pending | ⏳ |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | ⏳ pending | ⏳ |
| S1-05 | Single-instance mutex + bring-to-front | ⏳ pending | ⏳ |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | ⏳ pending | ⏳ |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | ⏳ pending | ⏳ |
| S1-08 | Unit tests for FileService + SettingsService | ⏳ pending | ⏳ |

---

## Completed Work (S1-01)

### ✅ Architecture & Scaffold
- WinUI 3 Packaged project structure created
- Solution file with main project + test project
- NuGet packages: WindowsAppSDK 1.5, CommunityToolkit.Mvvm 8.2, DI, DiffPlex, Markdig
- Package.appxmanifest with `broadFileSystemAccess` capability

### ✅ Dependency Injection
- DI container bootstrapped in App.xaml.cs
- All core services registered:
  - `IFileService` → `FileService` (transient)
  - `ISettingsService` → `SettingsService` (singleton)
  - `INormalizationService` → `NormalizationService` (transient, stub)
  - `IMemoryTrackerService` → `MemoryTrackerService` (transient, stub)
  - `INotificationService` → `NotificationService` (transient, stub)

### ✅ MVVM Wiring
- `MainViewModel` with `ObservableObject` base
- Commands: `LoadFileCommand`, `SaveFileCommand`, `NormalizeCommand`
- `SyncState` enum (Saved/Unsaved/Saving)

### ✅ Single-Instance Guard
- `AppInstance.FindOrRegisterForKey("singleton-notepad")` in App.xaml.cs
- Redirects second instance to bring existing window to front

### ✅ Service Implementations
- **FileService:** Read/write with retry logic (3 attempts, exponential backoff), auto-create with template, FileSystemWatcher support
- **SettingsService:** LocalSettings wrapper with typed get/set, PasswordVault for API keys
- **Model classes:** `ChangeRecord`, `AppSettings`, `NormalizationResult`, `LlmProviderConfig`

### ✅ Helper Classes
- `MonitorHelper`: P/Invoke for window positioning across monitors
- `WinUI.Interop`: Window handle interop for WinUI 3

### ✅ Views
- `App.xaml/cs`: DI bootstrap, exception handling
- `MainWindow.xaml/cs`: Window lifecycle, position persistence
- `MainView.xaml/cs:` MenuBar, CommandBar, Editor, StatusBar shell

### ✅ Test Project
- `FileServiceTests.cs`: 3 unit tests
- `SettingsServiceTests.cs`: 3 unit tests
- MSTest framework configured

### ⚠️ Build Note
Full XAML compilation requires Visual Studio 2022 with Windows App SDK components. All core services and logic compile successfully via `dotnet build`. Test report: `bmad/artifacts/test-results/S1-01-test-results.md`

---

## Strategic Dimensions

### Marketing
- Tagline locked: "one place for all notes"
- Visual: Fluent Design parity with Notepad Win11
- Distribution path: MSIX-packaged → potential Store

### Product
- S1-01 COMPLETE: WinUI 3 scaffold + DI + MVVM wiring done
- All core services implemented (FileService, SettingsService, stubs for others)
- Single-instance guard with AppInstance.FindOrRegisterForKey
- S1-02 NEXT: Refine FileService with auto-save debounce

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Implement S1-02 — FileService + auto-create + auto-save (2s debounce)

Focus on:
1. Debounce timer implementation in FileService
2. Auto-save on content change (2s idle)
3. Sync state tracking (Saved/Unsaved/Saving)
4. Unit tests for debounce behavior

---

## Phase Status

| Phase | Status |
|-------|--------|
| Planning | ✅ done |
| Development | 🔄 in_progress |
| Testing | ⏳ upcoming |
| Release | ⏳ upcoming |

---

*This report is auto-generated from `bmad/status.yaml`. Do not edit manually.*
