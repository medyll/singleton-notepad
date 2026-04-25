# Singleton Notepad

**One place for all notes.**

A minimalist Windows 11 desktop app for rapid note-taking, anchored on a single Markdown file. Combats note fragmentation with LLM-driven normalization of content using user-defined rules.

## Features (Planned)

- **Single-file discipline:** Reads/writes only `MY_SINGLETON_NOTEPAD.md`
- **Auto-save:** 2s debounce, no manual saving needed
- **LLM normalization:** Restructure notes using custom rules (Ollama, OpenAI, Anthropic)
- **Inline diff preview:** See changes before applying normalization
- **Change tracking:** Automatic backup and memory log
- **Native Win11 feel:** Fluent Design, MSIX-packaged

## Tech Stack

- **Framework:** WinUI 3 (Packaged / MSIX)
- **Language:** C# .NET 8
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **DI:** Microsoft.Extensions.DependencyInjection
- **LLM:** Strategy pattern (Ollama default, OpenAI/Anthropic optional)
- **Diff:** DiffPlex
- **Markdown:** Markdig

## Current Status

**Phase:** Development (Sprint 1 of 4)

### Sprint 1 — MVP Foundation ✅

- [x] WinUI 3 project scaffold with DI + MVVM wiring
- [ ] FileService with auto-create + auto-save
- [ ] SettingsService over LocalSettings
- [ ] MainWindow positioning (primary monitor)
- [ ] Single-instance enforcement
- [ ] MainView shell (MenuBar, CommandBar, Editor, StatusBar)
- [ ] SettingsView shell
- [ ] Unit tests for services

### Upcoming Sprints

**Sprint 2 — LLM Normalization**
- Ollama provider integration
- AGENTS.md rules loader
- Inline diff preview
- MEMORY.md tracking

**Sprint 3 — Polish**
- Markdown syntax highlighting
- OpenAI + Anthropic providers
- DPAPI key storage
- Theme switcher

**Sprint 4 — Advanced**
- FileSystemWatcher for external edits
- Versioned backups
- Plugin system
- Optional cloud sync

## Build Requirements

- Windows 11
- Visual Studio 2022 with:
  - Windows App SDK 1.5+
  - .NET 8 SDK
  - MSIX tooling

## Build Instructions

```bash
# Restore packages
dotnet restore

# Build (requires VS2022 for XAML compilation)
dotnet build

# Run tests
dotnet test
```

## Project Structure

```
SingletonNotepad/
├── SingletonNotepad/           # Main WinUI 3 project
│   ├── App.xaml.cs             # DI bootstrap, single-instance guard
│   ├── MainWindow.xaml.cs      # Window positioning, lifecycle
│   ├── Core/
│   │   ├── Services/           # IFileService, ISettingsService, etc.
│   │   ├── Providers/          # ILlmProvider strategy pattern
│   │   ├── Models/             # Data models
│   │   └── Helpers/            # MonitorHelper, Interop
│   ├── ViewModels/             # MainViewModel (MVVM)
│   └── Views/                  # MainView, SettingsView
├── SingletonNotepad.Tests/     # MSTest unit tests
└── bmad/                       # BMAD project tracking
    ├── status.yaml             # Project state
    ├── artifacts/
    │   ├── docs/               # PRD, Architecture
    │   └── test-results/       # Test reports
    └── intake-sources/         # Original project intent
```

## Roadmap

| Version | Goal | ETA |
|---------|------|-----|
| 0.1 | MVP (Sprint 1) | 2026-Q2 |
| 0.5 | LLM normalization (Sprint 2) | 2026-Q3 |
| 1.0 | Production ready (Sprint 3) | 2026-Q4 |
| 1.5+ | Advanced features (Sprint 4+) | 2027+ |

## License

MIT

---

**Status:** Active development. Not yet release-ready.
