# Singleton Notepad — Project Status

**Generated:** 2026-04-25  
**Phase:** development (75% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Active Story:** S1-07 — SettingsView shell with NavigationView  
**Last Completed:** S1-04 — MainWindow positioning ✅

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | ✅ complete | ✅ pass |
| S1-03 | SettingsService over LocalSettings (paths, window geometry) | ✅ complete | ✅ pass |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | ✅ complete | ✅ pass |
| S1-05 | Single-instance mutex + bring-to-front | ✅ complete | ✅ pass |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | ✅ complete | ✅ pass |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | 🔄 in_progress | ⏳ |
| S1-08 | Unit tests for FileService + SettingsService | ✅ complete | ✅ pass |

---

## Completed Work (S1-04)

### MonitorHelper Enhancements
- **IsWindowValidOnPrimaryMonitor()**: Validates window is at least 50% visible
- **EnsureValidWindowPosition()**: Returns valid position or centered fallback
- **GetPrimaryMonitorWorkArea()**: Returns bounds excluding taskbar
- **Removed unused P/Invokes**: Fixes audit issue (code cleanup)

### Edge Cases Handled
1. **Monitor Disconnected:** Saved position invalid → center on primary
2. **Resolution Changed:** Window partially off-screen → validate 50% rule
3. **Taskbar Position:** Uses WorkingArea for accurate centering
4. **Multi-Monitor:** Falls back to primary if position invalid
5. **Negative/Far Coordinates:** Treated as invalid, window centered

### MainWindow Integration
- Uses `EnsureValidWindowPosition()` before restoring
- Falls back to `CenterOnPrimaryMonitor()` when needed
- Preserves window size while adjusting position

### Tests (10 new)
- Valid/invalid coordinate detection
- Fully/partially/completely off-screen scenarios
- Centered fallback verification

---

## Strategic Dimensions

### Marketing
- Tagline locked: "one place for all notes"
- Visual: Fluent Design parity with Notepad Win11
- Distribution path: MSIX-packaged → potential Store

### Product
- S1-04 COMPLETE: MainWindow positioning with 50% visibility validation
- MonitorHelper with EnsureValidWindowPosition() fallback to center
- 6/8 Sprint 1 stories complete (75% done)
- S1-07 NEXT: SettingsView shell with NavigationView

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Implement S1-07 — SettingsView shell

Focus on:
1. Create SettingsView.xaml with NavigationView
2. Add Apparence and Fichiers panes
3. Bind to SettingsViewModel
4. Add navigation from MainView to SettingsView

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
