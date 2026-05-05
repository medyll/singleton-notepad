# BMAD Status — singleton-notepad

**Phase:** development · **Progress:** 10% · **Last Updated:** 2026-05-05

## Next Action
**Implement S1-02:** FileService + auto-create + auto-save (2s debounce)

## Sprint 1 — MVP Foundation (in_progress)

| Story | Status |
|-------|--------|
| S1-01: Scaffold WinUI 3 + DI + MVVM + AppInstance single-instance | ✅ done |
| S1-02: FileService + auto-create + auto-save (2s debounce) | 🔄 next |
| S1-03: SettingsService (LocalSettings + PasswordVault for API keys) | ⬜ pending |
| S1-04: MainWindow AppWindow positioning (DisplayArea, primary monitor) | ⬜ pending |
| S1-05: MainPage shell: MenuBar + CommandBar + Editor + StatusBar | ⬜ pending |
| S1-06: SettingsPage shell: NavigationView + Apparence/Fichiers panes | ⬜ pending |
| S1-07: Unit tests: FileService + SettingsService | ⬜ pending |

## Stack Migration
- **From:** Avalonia 11
- **To:** WinUI 3 (Windows App SDK 1.7)
- **Reason:** Native Win11 Fluent UX. Previous Avalonia code deleted. Clean restart.

## 3 Dimensions

### Marketing
A minimalist Windows 11 notepad — one file, zero friction, LLM-powered normalization.

### Product
WinUI 3 single-file Markdown editor with auto-save, settings, and LLM reorganization.

### Far Vision
The last note-taking app you'll ever need — one file, self-organizing, always in sync.
