# Intake Sources

## Files

- `SCRATCHPAD.md` — Full technical specification (724 lines, French) covering vision, features, WinUI 3 architecture, LLM integration, UI mockups, roadmap.

## Intent Reading

Singleton Notepad is a minimalist Windows desktop app (WinUI 3 / C#) for rapid note-taking centered on a **single Markdown file** (`MY_SINGLETON_NOTEPAD.md`). Its differentiator is **automatic content normalization via an external LLM** (Ollama / OpenAI / Anthropic) driven by user-defined rules in `NOTEPAD_SINGLETON_AGENTS.md`, with change tracking persisted to `NOTEPAD_SINGLETON_MEMORY.md`. Philosophy: *"one place for all notes"* — fight fragmentation across files and apps. UX inspiration: Windows 11 Notepad (Fluent Design, MenuBar + CommandBar + StatusBar, NavigationView for settings).

## Signals for PM

- **Stack locked**: WinUI 3, C#, MVVM (CommunityToolkit.Mvvm), Markdig, DiffPlex, MSTest + Playwright.
- **4-phase roadmap** already drafted: MVP (file I/O + window persistence) → LLM (Ollama + diff preview) → Polish (syntax + shortcuts + multi-provider) → Advanced (watcher + backups + plugins + cloud sync).
- **Inline diff preview** is a strong UX choice (in-editor, not modal) — preserve in PRD.
- **Single-instance mutex** + primary-monitor-only window are explicit constraints.
- **Open questions** to resolve with user: MEMORY format (MD vs JSON), auto-normalize rate limits, max file size warning, image handling, export/import.
- **No code yet** — greenfield; first sprint = scaffold WinUI 3 project + MVP file I/O.
