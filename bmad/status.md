# BMAD Status — singleton-notepad

**Last updated:** 2026-05-05
**Phase:** Sprint 3 — Polish & Production Readiness (in_progress)
**Progress:** 100% (S1+S2 complete, S3 in progress)

---

## Next Action

**Command:** `bmad-dev-story S3-02`
**Role:** Developer
**Task:** Implement S3-02: OpenAI + Anthropic providers (ILlmProvider implementation for cloud LLMs)

---

## Sprint Progress

### ✅ Sprint 1 — MVP Foundation (complete)
- S1-01 ✅ Scaffold WinUI 3 + DI + MVVM + named Mutex single-instance
- S1-02 ✅ FileService + auto-create + auto-save (2s debounce)
- S1-03 ✅ SettingsService (JSON %LocalAppData%\SingletonNotepad\settings.json)
- S1-04 ✅ MainWindow AppWindow positioning (DisplayArea, primary monitor)
- S1-05 ✅ MainPage shell: MenuBar + CommandBar + Editor + StatusBar
- S1-06 ✅ SettingsPage shell: NavigationView + Apparence/Fichiers panes
- S1-07 ✅ Unit tests: FileService + SettingsService

### ✅ Sprint 2 — LLM Normalization (complete)
- S2-01 ✅ ILlmProvider + OllamaProvider
- S2-02 ✅ NormalizationService (rules, rate-limit, diff)
- S2-03 ✅ MemoryTrackerService (MEMORY.md)
- S2-04 ✅ InlineDiffEditor control (diff preview)
- S2-05 ✅ Normalize triggers + ViewModel wiring
- S2-06 ✅ DefaultRules.md + AGENTS.md auto-create
- S2-07 ✅ Unit tests for Sprint 2 services (34 tests passing)

### 🔄 Sprint 3 — Polish & Production Readiness (in_progress)
- S3-01 ✅ Markdown toggle preview (Markdig) — toggle button in CommandBar
- S3-02 ⏳ OpenAI + Anthropic providers (in progress)
- S3-03 ⏳ DPAPI key storage for API keys
- S3-04 ⏳ FileSystemWatcher external-edit detection

### ⏳ Sprint 4 — Advanced (upcoming)
Versioned backups, custom rules plugins, optional cloud sync.

---

## Review Findings (Sprint 2 — FIX BEFORE S3-03)

**Critical:** 0
**Important:** 2
1. `NormalizationService` calls `AppendAsync` without await (fire-and-forget)
2. `OllamaProvider` uses static `HttpClient` instance (socket exhaustion risk)

**Minor:** 16 (test assertion style warnings)

---

## Build

> ⚠️ Cibler le `.csproj` directement — la `.slnx` ne propage pas `-p:Platform`.
> ```
> dotnet build SingletonNotepad/SingletonNotepad.csproj -p:Platform=x64
> dotnet test SingletonNotepad.Tests/SingletonNotepad.Tests.csproj -p:Platform=x64
> ```
> Même info dans : `README.md` · `SingletonNotepad.csproj` (commentaire) · `SingletonNotepad.slnx` (commentaire) · `ARCHITECTURE.md §6`

---

## Stack

- **WinUI 3** (Windows App SDK **2.0.1**) · .NET 10 · C#
- CommunityToolkit.Mvvm 8.4 · MSTest · DiffPlex 1.9 · Markdig 0.40

---

## Summary

**Marketing:** Single-file Markdown notepad with LLM normalization. Sprint 1 MVP complete (WinUI 3, file I/O, window persist, settings). Sprint 2 adds Ollama-powered content normalization.

**Product:** S3 adds syntax highlighting toggle, cloud LLM providers (OpenAI/Anthropic), DPAPI-encrypted key storage, and FileSystemWatcher-based external edit detection.

**Vision:** Phase 3 (Sprint 3-4): Markdown syntax highlighting, OpenAI/Anthropic providers, DPAPI key storage, FileSystemWatcher external-edit, cloud sync.