# Singleton Notepad — Project Status

**Generated:** 2026-04-25  
**Phase:** development (50% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Active Story:** S1-03 — SettingsService over LocalSettings (paths, window geometry)  
**Last Completed:** Audit fixes (3 important issues resolved) ✅

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | ✅ complete | ✅ pass |
| S1-03 | SettingsService over LocalSettings (paths, window geometry) | 🔄 in_progress | ⏳ |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | ⏳ pending | ⏳ |
| S1-05 | Single-instance mutex + bring-to-front | ✅ complete | ✅ pass |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | ✅ complete | ✅ pass |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | ⏳ pending | ⏳ |
| S1-08 | Unit tests for FileService + SettingsService | ✅ complete | ✅ pass |

---

## Code Audit Status

**Health Score:** 9/10 ⭐ (improved from 8/10)

### Issues Resolved

| Issue | Severity | Status |
|-------|----------|--------|
| FileService `async void` timer callback | 🟡 Important | ✅ FIXED |
| SettingsService catch-all exception | 🟡 Important | ✅ FIXED |
| MainWindow bounds validation missing | 🟡 Important | ✅ FIXED |

### Remaining Minor Issues (Backlog)

1. IFileService missing `IDisposable` declaration
2. MainViewModel event subscriptions without cleanup
3. MonitorHelper unused P/Invoke declarations
4. App.xaml.cs commented-out LLM provider code
5. Missing Watch callback integration test

**Audit Report:** `bmad/artifacts/audit/S1-01-code-audit.md`  
**Fix Test Results:** `bmad/artifacts/test-results/audit-fix-results.md`

---

## Completed Work

### ✅ S1-01: Architecture & Scaffold
- WinUI 3 Packaged project structure
- DI container with all core services
- MVVM wiring with CommunityToolkit.Mvvm
- Single-instance guard with AppInstance
- FileService with retry logic (3 attempts, exponential backoff)
- SettingsService with PasswordVault (DPAPI)

### ✅ S1-02: FileService Debounce
- 2s debounce timer on content changes
- `FileSaved` / `SaveFailed` events for ViewModel integration
- Auto-save on keystroke with timer reset
- `_loading` guard prevents save-on-load
- 8 unit tests covering debounce behavior

### ✅ Audit Fixes
- **FileService:** Extracted `async void` to `async Task SaveAndNotifyAsync`
- **SettingsService:** Added exception filtering (`when` clause for OutOfMemory/StackOverflow)
- **MainWindow:** Added `PresenterState` validation and minimum bounds check (400x300)

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
- CODE AUDIT: 9/10 health score (0 critical, 0 important, 5 minor)
- All 3 important issues fixed: async void, exception filtering, window validation
- S1-03 NEXT: SettingsService refinement (paths, window geometry persistence)

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Implement S1-03 — SettingsService over LocalSettings

Focus on:
1. Add strongly-typed settings model (AppSettings)
2. Persist singleton file path and rules file path
3. Persist window geometry (already done, verify integration)
4. Add unit tests for settings persistence

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
