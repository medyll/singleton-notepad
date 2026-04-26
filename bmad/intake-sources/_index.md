# Intake Sources

## Files

- `SCRATCHPAD.md` — Full technical specification, framework-agnostic, covering vision, features, architecture, LLM integration, UI mockups, roadmap.

---

Singleton Notepad is a minimalist cross-platform desktop app (Avalonia / C#) for rapid note-taking centered on a **single Markdown file** (`MY_SINGLETON_NOTEPAD.md`). Its differentiator is **automatic content normalization via an external LLM** (Ollama / OpenAI / Anthropic) driven by user-defined rules in `NOTEPAD_SINGLETON_AGENTS.md`, with change tracking persisted to `NOTEPAD_SINGLETON_MEMORY.md`. Philosophy: *"one place for all notes"* — fight fragmentation across files and apps. UX inspiration: Fluent Design with Menu + Toolbar + StatusBar, TabControl for settings.

---

- **Stack**: Avalonia 11, C# .NET 8, MVVM (CommunityToolkit.Mvvm), Markdig, DiffPlex, MSTest.

---

- **Status**: Sprint 1 complete — MVP functional (edit, save, settings navigation)
