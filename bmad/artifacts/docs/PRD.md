# Singleton Notepad — Product Requirements Document

**Status:** Draft v1 · **Owner:** PM · **Date:** 2026-04-25
**Source:** `bmad/intake-sources/INTENT.md` · `bmad/intake-sources/SCRATCHPAD.md`

---

## 1. Vision

A minimalist Windows 11 desktop app for rapid note-taking, anchored on **a single Markdown file**. Combats note fragmentation. Differentiated by **LLM-driven normalization** of content using user-defined rules, with inline diff preview before apply.

Tagline: *"One place for all notes."*

## 2. Target User

Solo knowledge workers and developers on Windows 11 who:
- Want a Notepad-like UX (instant launch, zero friction)
- Are comfortable running local LLMs (Ollama) but may use cloud LLMs (OpenAI / Anthropic)
- Value reorganization without losing the freedom to scratch quickly

## 3. Goals (success criteria)

| # | Goal | Metric |
|---|------|--------|
| G1 | Frictionless capture | Cold start < 2s; auto-save 2s debounce |
| G2 | Single-file discipline | App reads/writes only `MY_SINGLETON_NOTEPAD.md` |
| G3 | Trustworthy normalization | 100% of normalizations show inline diff before apply |
| G4 | Native Win11 feel | Fluent Design parity with Notepad Win11 |
| G5 | Provider portability | Ollama works offline; OpenAI/Anthropic swap-in via settings |

## 4. Non-Goals (v1)

- Multi-file workspaces / tabs
- Cloud sync (deferred to Phase 4)
- Mobile / cross-platform
- Real-time collaboration
- Plugin system (deferred to Phase 4)

## 5. Functional Requirements

### 5.1 File handling
- FR-01 Read/write a single Markdown file at a configurable path.
- FR-02 Auto-save with 2s debounce.
- FR-03 Detect external modifications via `FileSystemWatcher`; offer reload (Phase 1.5 — see roadmap).
- FR-04 Auto-create the file with an empty template if absent.

### 5.2 Window & instance
- FR-05 Persist window X/Y/W/H across sessions; restore on **primary monitor only**.
- FR-06 If saved position is off-screen, fall back to centered on primary monitor.
- FR-07 Single-instance enforcement via named mutex; bring existing window to front on second launch.

### 5.3 Editor UX
- FR-08 MenuBar (Édition / Affichage / Paramètres).
- FR-09 CommandBar with: Open, Save, Normalize, Copy.
- FR-10 StatusBar: encoding · line/col · sync state · last-normalized timestamp.
- FR-11 Keyboard: `Ctrl+O/S/N/Q`, `F5` (reload), `Ctrl+,` (settings).
- FR-12 Markdown syntax highlighting (Phase 3).

### 5.4 LLM normalization
- FR-13 Rules file `NOTEPAD_SINGLETON_AGENTS.md` consumed at normalize time.
- FR-14 Triggers: manual button, on-close (toggle), on-idle (configurable minutes).
- FR-15 Scope: full document **or** selection-with-context.
- FR-16 **Inline diff preview** in the editor (no modal): added (green), deleted (red strikethrough), modified (orange). Apply / Cancel in-place.
- FR-17 Providers: Ollama (default), OpenAI, Anthropic. Endpoint, model, API key configurable per provider.
- FR-18 Backup the previous content before applying normalization.

### 5.5 Change tracking
- FR-19 Append change records to `NOTEPAD_SINGLETON_MEMORY.md` (timestamp, type, lines changed, preview, backup ref).
- FR-20 Snapshot frequency configurable: `OnNormalize` (default), `Hourly`, `Daily`, `Off`.

### 5.6 Settings
- FR-21 NavigationView with sections: Apparence / Fichiers / Normalisation / Historique.
- FR-22 Theme: Clair / Sombre / Système.
- FR-23 All settings persisted to `ApplicationData.LocalSettings`.

## 6. Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| NFR-01 | Cold start < 2s on baseline hardware |
| NFR-02 | LLM calls async; UI never blocks |
| NFR-03 | WCAG AA contrast; full keyboard nav; AutomationProperties on all interactive controls |
| NFR-04 | Robustness to FileIOException (3 retries, exponential backoff) |
| NFR-05 | LLM HTTP timeout 30s with user-visible toast on failure |
| NFR-06 | LLM response sanitization (basic Markdown validity check) before write |

## 7. Resolved Open Questions

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| Q1 | MEMORY.md format | **Markdown** (human-readable, append-only) | Aligns with single-file philosophy; user can read it directly |
| Q2 | Auto-normalize rate limit | **Min 5 min between auto-normalizes** + skip if file unchanged | Caps LLM cost without surprising the user |
| Q3 | File-size warning | Toast when file > **500 KB** or > **10k lines** (whichever first) | Approximate Ollama default context budget |
| Q4 | Image handling | **External links only** in v1 | Keeps the file portable and small; base64 inflates diffs |
| Q5 | Export / Import | **Out of scope v1** — file is plain `.md`, copy/move it manually | YAGNI; revisit in Phase 4 alongside cloud sync |
| Q6 | API key storage | **DPAPI-encrypted** (`ProtectedData` / `PasswordVault`) | Cloud keys are sensitive; defense-in-depth |
| Q7 | WinUI 3 template | **Packaged (MSIX)** with WindowsAppSDK 1.5+ | Enables `broadFileSystemAccess`, simpler distribution, future Store path |

## 8. Architecture Snapshot

```
SingletonNotepad/
├── App.xaml.cs                     # DI bootstrap, mutex
├── MainWindow.xaml(.cs)             # Window positioning, monitor logic
├── Core/
│   ├── Services/                    # FileService, NormalizationService,
│   │                                # MemoryTrackerService, SettingsService
│   ├── Providers/                   # OllamaProvider, OpenAiProvider, AnthropicProvider
│   ├── Models/                      # AppSettings, NormalizationRule, ChangeRecord
│   └── Helpers/                     # MonitorHelper, PathHelper, DpapiHelper
├── Views/
│   ├── MainView.xaml                # MenuBar + CommandBar + Editor + StatusBar
│   ├── SettingsView.xaml            # NavigationView
│   └── Controls/
│       ├── MarkdownEditor.xaml      # TextBox + syntax highlighter
│       └── InlineDiffEditor.xaml    # RichTextBlock-based diff overlay
└── Resources/
    └── DefaultRules.md              # Seed for AGENTS.md
```

**Stack:** WinUI 3 (Packaged) · C# · CommunityToolkit.Mvvm 8.2 · Markdig 3 · DiffPlex 1.9 · MSTest + Playwright.

## 9. Roadmap & Sprints

### Sprint 1 — MVP Foundation (this sprint)
**Goal:** scaffold + file I/O + window persistence + auto-save. **No LLM.**

| ID | Story | Role | Estimate |
|----|-------|------|----------|
| S1-01 | Scaffold WinUI 3 (Packaged) project + DI + MVVM wiring | Architect → Dev | M |
| S1-02 | `FileService` + auto-create + auto-save (2s debounce) | Dev | M |
| S1-03 | `SettingsService` over `LocalSettings` (paths, window geometry) | Dev | S |
| S1-04 | `MainWindow` positioning (primary monitor, restore/center fallback) | Dev | M |
| S1-05 | Single-instance mutex + bring-to-front | Dev | S |
| S1-06 | MainView shell: MenuBar + CommandBar + Editor + StatusBar | Designer → Dev | M |
| S1-07 | SettingsView shell: NavigationView + Apparence/Fichiers panes | Designer → Dev | M |
| S1-08 | Unit tests for FileService + SettingsService | Tester | S |

**Sprint 1 exit criteria:** App launches, opens/edits/saves the singleton file, remembers window position, only one instance allowed, settings persist. All S1 stories `complete` with `tests_executed: true, test_result: pass`.

### Sprint 2 — LLM Normalization
Ollama provider, AGENTS.md rules loader, inline diff preview, MEMORY.md tracking, manual + on-close + on-idle triggers.

### Sprint 3 — Polish
Markdown syntax highlighting, full keyboard map, toast notifications, OpenAI + Anthropic providers, DPAPI key storage, theme switcher.

### Sprint 4 — Advanced
FileSystemWatcher external-edit detection, versioned backups, custom rules plugins, optional cloud sync.

## 10. Risks

| Risk | Mitigation |
|------|-----------|
| LLM returns malformed Markdown | Sanitize + diff preview + Cancel always available |
| WinUI 3 single-instance is non-trivial in Packaged apps | Use `AppInstance.FindOrRegisterForKey` (Win App SDK), fallback to mutex |
| Inline diff in `RichTextBlock` performance on large files | Diff per-paragraph; lazy render past viewport |
| Ollama not installed on user machine | First-run wizard detects + links to install; OpenAI/Anthropic remain |

## 11. Acceptance for v1.0 (end of Sprint 3)

- All Sprint 1 + 2 + 3 stories `complete` with passing tests.
- Manual smoke test: open → edit → save → normalize (Ollama) → preview → apply → MEMORY.md updated.
- Cold start < 2s measured on baseline (i5-class, SSD).
- No P0/P1 bugs in `qa.bugs`.
