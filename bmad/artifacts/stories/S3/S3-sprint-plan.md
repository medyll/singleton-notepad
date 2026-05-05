# Sprint 3 — Polish & Production Readiness

**PM:** Clément
**Date:** 2026-05-05
**Goal:** Ship a production-ready notepad with syntax highlighting, cloud LLM providers, secure key storage, and external-edit detection.

---

## Stories

### S3-01 — Markdown Syntax Highlighting
- Integrate `Markdig` into the TextBox rendering pipeline
- Syntax colors: headings (blue bold), bold/italic (weight/bold), code blocks (gray bg), links (purple), lists (bullets colored)
- Toggle: Affichage → "Coloration syntaxique" checkbox
- Performance: only re-render on content change or scroll viewport
- Keyboard: Tab inserts 2 spaces in lists, Enter continues list or new paragraph

### S3-02 — OpenAI + Anthropic Providers
- Create `OpenAiProvider` (model: `gpt-4o-mini` default), `AnthropicProvider` (model: `claude-3-5-haiku` default)
- Both implement `ILlmProvider`
- Register all 3 providers in DI; user selects active provider in Settings
- Provider-specific request/response mapping (Anthropic uses `messages` array format, OpenAI uses `messages` too but different roles)
- API key stored via DPAPI (see S3-03)

### S3-03 — DPAPI Key Storage
- Store API keys for OpenAI/Anthropic using `ProtectedData` (DPAPI) via `DataProtectionScope = CurrentUser`
- Encrypt before writing to settings JSON, decrypt on load
- Fallback: if DPAPI unavailable, prompt user and store in PasswordVault
- Prompt on first provider switch: "Store key securely?" Yes/No

### S3-04 — FileSystemWatcher External-Edit Detection
- Wrap `FileSystemWatcher` in `FileService` or a dedicated `IFileWatcherService`
- On external change: set `ExternalChangeDetected` event, fire toast notification "File changed externally — click to reload"
- Debounce: 500ms to avoid duplicate events on save
- Conflict resolution: if user has unsaved edits, show "Reload and lose changes?" dialog
- Also trigger auto-reload if the external change happened after last save (i.e., not our own save)

---

## Dependencies
- `Markdig 3.x` — already in stack
- `System.Security.Cryptography.ProtectedData` (built-in, .NET 6+)

## Exit Criteria
- All 4 stories done with passing tests
- Toggle syntax highlighting → instant color change, no flicker
- Switch provider to OpenAI → normalize works, key stored encrypted
- External edit → toast shown within 1s, reload works correctly
- No P0/P1 bugs, cold start < 2s