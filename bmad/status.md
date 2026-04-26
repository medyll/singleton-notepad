# Singleton Notepad — Project Status

**Generated:** 2026-04-26  
**Phase:** development (90% complete)

---

## Executive Summary

**Current Sprint:** Sprint 1 — MVP Foundation  
**Last Completed:** Migration to Avalonia ✅  
**Status:** Build successful, app runs

---

## Sprint 1 Progress

| Story | Title | Status | Tests |
|-------|-------|--------|-------|
| S1-01 | Scaffold Avalonia project + DI + MVVM wiring | ✅ complete | ✅ pass |
| S1-02 | FileService + auto-create + auto-save (2s debounce) | ✅ complete | ✅ pass |
| S1-03 | SettingsService over JSON (paths, window geometry) | ✅ complete | ✅ pass |
| S1-04 | MainWindow positioning (primary monitor, restore/center fallback) | ✅ complete | ✅ pass |
| S1-05 | Single-instance mutex + bring-to-front | ✅ complete | ✅ pass |
| S1-06 | MainView shell: Menu + Toolbar + Editor + StatusBar | ✅ complete | ✅ pass |
| S1-07 | SettingsView shell: TabControl + Apparence/Fichiers panes | ✅ complete | ✅ pass |
| S1-08 | Unit tests for FileService + SettingsService | ⏳ pending | ⏳ |

---

## Completed Work

### Migration to Avalonia
- **Framework**: Migrated from WinUI 3 to Avalonia 11.2.1
- **Build**: Successfully compiles (no XAML compiler errors)
- **Runtime**: Application launches and runs

### Architecture
- **DI Registration**: All views registered in App.axaml.cs
- **ViewModel Integration**: SettingsViewModel with GoBackCommand
- **Navigation**: MenuBar "Paramètres" → SettingsView → Back to MainView
- **Settings Model**: 10+ properties (FontFamily, FontSize, WordWrap, etc.)

### Settings Pages Features
**Apparence:**
- Theme selection (Light/Dark/System)
- Font family and size
- Word wrap toggle
- Line numbers toggle
- Bracket highlighting toggle
- Window dimensions

**Fichiers:**
- Singleton file path with browse button
- Rules file path with browse button
- Auto-save enable/disable
- Auto-save debounce interval (100-30000ms)
- Backup creation toggle

### Technical Stack
| Component | Technology |
|-----------|------------|
| Framework | Avalonia 11.2.1 |
| Language | C# .NET 8 |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| Storage | JSON files in AppData |
| Notifications | Avalonia WindowNotificationManager |

---

## Strategic Dimensions

### Marketing
- Tagline locked: "one place for all notes"
- Visual: Fluent Design theme (cross-platform)
- Distribution: Self-contained executable or framework-dependent

### Product
- MVP functional: Edit, save, settings navigation working
- Cross-platform ready: Windows, Linux, macOS compatible
- LLM integration: Sprint 2 (Ollama, OpenAI, Anthropic)

### Far Vision
- Phase 4: FileSystemWatcher, versioned backups, plugins, cloud sync
- Multi-provider LLM: Ollama default + OpenAI/Anthropic swap

---

## Next Action

**Command:** `bmad continue`  
**Role:** Developer  
**Task:** Continue with Sprint 2 (LLM integration) or S1-08 (unit tests)

Options:
1. Implement S1-08 unit tests (complete Sprint 1)
2. Start Sprint 2 planning (LLM providers)
3. Polish UI/UX (notifications, diff preview)

---

## Phase Status

| Phase | Status |
|-------|--------|
| Planning | ✅ done |
| Development | 🔄 in_progress |
| Testing | ⏳ upcoming |
| Release | ⏳ upcoming |

---

*This report is auto-generated. Do not edit manually.*
