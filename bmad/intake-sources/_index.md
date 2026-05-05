# Intake Sources

## Files

- `SCRATCHPAD.md` — Full technical specification covering vision, features, architecture, LLM integration, UI mockups, roadmap.
- `INTENT.md` — Original intent document. Philosophy, target users, open design questions.

---

SingletonNotepad is a minimalist Windows 11 desktop app for rapid note-taking centered on a **single Markdown file** (`MY_SINGLETON_NOTEPAD.md`). Its differentiator is **automatic content normalization via an external LLM** (Ollama / OpenAI / Anthropic) driven by user-defined rules in `NOTEPAD_SINGLETON_AGENTS.md`, with change tracking persisted to `NOTEPAD_SINGLETON_MEMORY.md`. Philosophy: *"one place for all notes"* — fight fragmentation across files and apps. UX: Fluent Design with MenuBar + CommandBar + StatusBar, NavigationView for settings.

---

## Confirmed Stack

| Layer | Choice |
|-------|--------|
| UI | WinUI 3 / Windows App SDK **2.0.1** |
| Runtime | .NET 10 (`net10.0-windows10.0.26100.0`) |
| MVVM | CommunityToolkit.Mvvm 8.3.2 (source generators) |
| DI | Microsoft.Extensions.DependencyInjection 8.0.1 |
| Diff | DiffPlex 1.9.0 |
| Markdown | Markdig 0.40.0 |
| Tests | MSTest |
| Packaging | MSIX single-project (`EnableMsixTooling=true`) |

**Not Avalonia.** The SCRATCHPAD.md originally mentioned Avalonia — that was a draft artifact. Decision confirmed: WinUI3 native for Win11 Fluent UX. No cross-platform requirement.

---

## Status

Clean reset 2026-05-05. Sprint 1 not started. See `../status.md`.
