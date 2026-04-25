# Singleton Notepad — Project Status

**Generated:** 2026-04-25  
**Phase:** development (60% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Active Story:** S1-04 — MainWindow positioning (primary monitor, restore/center fallback)  
**Last Completed:** S1-03 — SettingsService over LocalSettings ✅

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | ✅ complete | ✅ pass |
| S1-03 | SettingsService over LocalSettings (paths, window geometry) | ✅ complete | ✅ pass |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | 🔄 in_progress | ⏳ |
| S1-05 | Single-instance mutex + bring-to-front | ✅ complete | ✅ pass |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | ✅ complete | ✅ pass |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | ⏳ pending | ⏳ |
| S1-08 | Unit tests for FileService + SettingsService | ✅ complete | ✅ pass |

---

## Completed Work (S1-03)

### SettingsService Enhancements
- **GetSettings() / SaveSettings()**: Strongly-typed AppSettings with JSON serialization
- **Default Values**: Sensible defaults for all settings
  - SingletonFilePath: Documents/MY_SINGLETON_NOTEPAD.md
  - RulesFilePath: Documents/NOTEPAD_SINGLETON_AGENTS.md
  - Theme: System, AutoSaveDebounceMs: 2000ms
- **Backward Compatibility**: Migrates legacy individual window settings
- **ResetToDefaults()**: Clears all settings and returns to defaults

### SettingsViewModel
- **Properties**: Bound to AppSettings with change notification
- **Validation**: Path validation, debounce range (100-30000ms), window size (min 400x300)
- **Commands**:
  - SaveSettingsCommand: Validates and persists settings
  - ResetToDefaultsCommand: Resets to factory defaults
- **Error Display**: ValidationError string + HasValidationError flag

### Integration
- **MainWindow**: Uses GetSettings() for position persistence
- **MainViewModel**: Initializes FileService path from settings
- **DI Registration**: SettingsViewModel registered in App.xaml.cs

### Tests (6 new)
- AppSettings model tests: default values, property round-trip
- SettingsService integration: defaults, serialization, reset, window geometry

---

## Strategic Dimensions

### Marketing
- Tagline locked: "one place for all notes"
- Visual: Fluent Design parity with Notepad Win11
- Distribution path: MSIX-packaged → potential Store

### Product
- S1-03 COMPLETE: SettingsService with JSON serialization, defaults, validation
- SettingsViewModel with path validation and reset-to-defaults command
- 5/8 Sprint 1 stories complete (S1-01, S1-02, S1-03, S1-05, S1-06, S1-08 done)
- S1-04 NEXT: MainWindow positioning with primary monitor validation

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Implement S1-04 — MainWindow positioning

Focus on:
1. Verify primary monitor detection works correctly
2. Add fallback to center when saved position is off-screen
3. Test window restore on different monitor configurations
4. Add unit tests for MonitorHelper

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
