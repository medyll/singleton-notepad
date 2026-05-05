# BMAD Status — singleton-notepad

**Last updated:** 2026-05-05
**Phase:** Development (in_progress)
**Progress:** 80%

---

## Next Action

**Command:** `bmad-dev-story S2-03`
**Role:** Developer
**Task:** Implement S2-03: MemoryTrackerService

---

## Sprint Progress

### ✅ Sprint 1 — MVP Foundation (complete)
- S1-01 ✅ Scaffold WinUI 3 + DI + MVVM + AppInstance single-instance
- S1-02 ✅ FileService + auto-create + auto-save (2s debounce)
- S1-03 ✅ SettingsService (LocalSettings + PasswordVault for API keys)
- S1-04 ✅ MainWindow AppWindow positioning (DisplayArea, primary monitor)
- S1-05 ✅ MainPage shell: MenuBar + CommandBar + Editor + StatusBar
- S1-06 ✅ SettingsPage shell: NavigationView + Apparence/Fichiers panes
- S1-07 ✅ Unit tests: FileService + SettingsService

### 🔄 Sprint 2 — LLM Normalization (in_progress)
- S2-01 ✅ ILlmProvider + OllamaProvider
- S2-02 ✅ NormalizationService (rules, rate-limit, diff)
- S2-03 ⏳ MemoryTrackerService (MEMORY.md)
- S2-04 ⏳ InlineDiffEditor control (diff preview)
- S2-05 ⏳ Normalize triggers + ViewModel wiring
- S2-06 ⏳ DefaultRules.md + AGENTS.md auto-create
- S2-07 ⏳ Unit tests for Sprint 2 services

### ⏳ Sprint 3 — Polish (upcoming)
Markdown syntax highlighting, full keyboard map, toast notifications, OpenAI + Anthropic providers, DPAPI key storage, theme switcher.

### ⏳ Sprint 4 — Advanced (upcoming)
FileSystemWatcher external-edit detection, versioned backups, custom rules plugins, optional cloud sync.

---

## Stack

- **WinUI 3** (Windows App SDK 2.0.1) · .NET 10 · C#
- CommunityToolkit.Mvvm 8.4 · MSTest 4.0 · DiffPlex 1.9
- Planned: Markdig 3.x

---

## Summary

**Marketing:** Single-file Markdown notepad with LLM normalization. Sprint 1 MVP complete (WinUI 3, file I/O, window persist, settings). Sprint 2 adds Ollama-powered content normalization.

**Product:** S2 builds normalization pipeline: ILlmProvider strategy, NormalizationService with rules/rate-limit/diff, MemoryTracker, InlineDiffEditor control, 3 trigger modes.

**Vision:** Phase 3 (Sprint 3-4): Markdown syntax highlighting, OpenAI/Anthropic providers, DPAPI key storage, FileSystemWatcher external-edit, cloud sync.
