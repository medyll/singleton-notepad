---
level: full
source: SCRATCHPAD.md
---

# Singleton Notepad — Intent

## Purpose

A minimalist Windows desktop application for rapid note-taking, designed around a **single Markdown file** as the unique destination for all notes. Combats note fragmentation across multiple files and apps. Differentiated by **LLM-driven automatic normalization**: user-defined rules reorganize content (by date, tags, priority, etc.) on demand or on triggers, with full diff preview before apply.

Tagline: *"One place for all notes."*

## Users

- Solo knowledge workers / developers who keep scratch notes and want them auto-organized.
- Users comfortable running local LLMs (Ollama) but also wanting cloud LLM options (OpenAI, Anthropic).
- Windows 11 users expecting native Fluent Design UX (the app mirrors Notepad Win11 visual codes).

## Features

### Core (MVP)
- Read/write a **single configurable Markdown file** (`MY_SINGLETON_NOTEPAD.md`).
- Persistent window position (X/Y/W/H) on **primary monitor only**.
- **Single-instance enforcement** via named mutex.
- Auto-save (2s debounce) with sync indicator in status bar.
- Settings stored in `ApplicationData.LocalSettings`.

### LLM Normalization
- Rules file: `NOTEPAD_SINGLETON_AGENTS.md` (user-defined reorganization instructions).
- Triggers: manual button, on-close, on-idle (configurable minutes).
- Scope: full document OR selection only (with surrounding context).
- Providers: Ollama (default, local), OpenAI, Anthropic.
- **Inline diff preview** in the editor itself (no modal): green = added, red strikethrough = deleted, orange = modified. `Apply` / `Cancel` buttons in-place.

### Change Tracking
- `NOTEPAD_SINGLETON_MEMORY.md`: timestamped history (manual edits + auto-normalizations + backup refs).
- Pre-normalization backups; configurable snapshot frequency.

### UX Surface
- **MenuBar**: Édition / Affichage / Paramètres.
- **CommandBar** (toolbar): Open, Save, Normalize, Copy.
- **StatusBar**: encoding, line/col, sync state, last-normalized timestamp.
- **SettingsView** (NavigationView): Apparence / Fichiers / Normalisation / Historique.
- Keyboard: `Ctrl+O/S/N/Q`, `F5`, `Ctrl+,`.

## Context

### Stack (locked)
- **Framework**: WinUI 3 (Windows App SDK ~1.5).
- **Language**: C#.
- **MVVM**: CommunityToolkit.Mvvm 8.2.2.
- **Markdown parsing**: Markdig 3.x.
- **Diff**: DiffPlex 1.9.
- **Tests**: MSTest + Playwright.
- **Capabilities**: `internetClient`, `broadFileSystemAccess`.

### Architecture
Layered Core/Views split: `Core/Services/` (file, normalization, memory tracker, settings), `Core/Models/`, `Core/Helpers/` (monitor, path), `Views/` (Main, Settings, Controls), `Resources/DefaultRules.md`.

### Non-functional
- Cold start < 2s.
- LLM calls async, never block UI.
- File-modified-externally detection via `FileSystemWatcher` (proposed Phase 4).
- WCAG-conformant contrast, full keyboard nav, AutomationProperties.

### Roadmap (4 phases)
1. **MVP** — file I/O, window persistence, settings, auto-save.
2. **LLM** — Ollama integration, rules file, inline diff preview, MEMORY.md tracking.
3. **Polish** — Markdown syntax highlighting, shortcuts, toast notifications, OpenAI/Anthropic.
4. **Advanced** — FileSystemWatcher, versioned backups, custom-rules plugins, optional cloud sync (OneDrive/Dropbox).

## Questions ⚠

1. ⚠ MEMORY.md format — human-readable Markdown or JSON for parseability? (spec leans MD)
2. ⚠ Rate limit on auto-normalize to control LLM API cost?
3. ⚠ File-size threshold to warn the user (LLM context limits)?
4. ⚠ Markdown image handling — inline base64 or external links only?
5. ⚠ Export/Import path for migration to other note apps?
6. ⚠ API key storage — plain `LocalSettings` or DPAPI-encrypted? (spec ambiguous: "chiffrées si sensible")
7. ⚠ Spec mentions `MainWindow.xaml.cs` and `App.xaml.cs` but no `.csproj` template choice (Packaged vs Unpackaged WinUI 3) — needs decision before scaffold.
