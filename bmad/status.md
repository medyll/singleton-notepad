# Singleton Notepad — Project Status

**Generated:** 2026-04-25  
**Phase:** development (45% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Active Story:** Code audit remediation (S1-01)  
**Last Completed:** S1-01 — Scaffold WinUI 3 project + DI + MVVM wiring ✅

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | ✅ complete | ✅ pass |
| S1-03 | SettingsService over LocalSettings (paths, window geometry) | ⏳ pending | ⏳ |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | ⏳ pending | ⏳ |
| S1-05 | Single-instance mutex + bring-to-front | ⏳ pending | ⏳ |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | ⏳ pending | ⏳ |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | ⏳ pending | ⏳ |
| S1-08 | Unit tests for FileService + SettingsService | ✅ complete | ✅ pass |

---

## Code Audit Results (S1-01)

**Auditor:** Reviewer (Vera)  
**Health Score:** 8/10 ⭐  
**Report:** `bmad/artifacts/audit/S1-01-code-audit.md`

### Issues Summary

| Severity | Count | Status |
|----------|-------|--------|
| 🔴 Critical | 0 | — |
| 🟡 Important | 3 | To fix |
| 🟢 Minor | 5 | Backlog |

### Important Issues (To Fix Before Sprint 2)

1. **FileService: `async void` in `OnDebounceElapsed`** — Cannot be tested properly, exceptions uncatchable
2. **SettingsService: Catch-all exception swallowing** — Should filter specific exceptions
3. **MainWindow: No validation of saved bounds** — Could restore invalid window state

### Minor Issues (Backlog)

4. IFileService missing `IDisposable` declaration
5. MainViewModel event subscriptions without cleanup
6. MonitorHelper unused P/Invoke declarations
7. App.xaml.cs commented-out LLM provider code
8. Missing Watch callback integration test
9. Namespace inconsistency in MainViewModel

---

## Completed Work

### ✅ S1-01: Architecture & Scaffold
- WinUI 3 Packaged project structure
- DI container with all core services
- MVVM wiring with CommunityToolkit.Mvvm
- Single-instance guard
- FileService with retry logic
- SettingsService with PasswordVault

### ✅ S1-02: FileService Debounce
- 2s debounce timer on content changes
- `FileSaved` / `SaveFailed` events
- ViewModel integration with auto-save
- `_loading` guard prevents save-on-load
- 8 unit tests (including debounce behavior)

### ✅ S1-08: Unit Tests
- FileServiceTests: 8 tests (round-trip, debounce, retry, watcher)
- SettingsServiceTests: 3 tests (get/set, type safety, defaults)
- Test isolation with temp files and cleanup

---

## Strategic Dimensions

### Marketing
- Tagline locked: "one place for all notes"
- Visual: Fluent Design parity with Notepad Win11
- Distribution path: MSIX-packaged → potential Store

### Product
- S1-01 COMPLETE: WinUI 3 scaffold + DI + MVVM wiring done
- CODE AUDIT: 8/10 health score (0 critical, 3 important, 5 minor)
- All core services implemented with retry logic, debounce, DPAPI
- Audit action items: fix async void, exception filtering, window validation

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Fix audit issues in S1-01 code

Focus on:
1. Convert `async void` to `async Task` in FileService
2. Add exception filtering in SettingsService.GetApiKey
3. Add window state validation in MainWindow.OnClosed

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
