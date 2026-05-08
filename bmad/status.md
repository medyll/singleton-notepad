# BMAD Status — singleton-notepad

**Last updated:** 2026-05-08
**Phase:** development (Sprint 5)
**Active Role:** Developer

---

## Current Sprint: S5 — Chat & Model UX

| ID | Story | Status |
|----|-------|--------|
| S5-01 | Chat bubble — floating LLM chat panel (minimizable, context-aware) | 🔲 todo |
| S5-02 | Model management — dynamic provider/model selector in Settings | ✅ done |
| S5-03 | Spell check — configurable, auto-detect language | ✅ done |
| S5-04 | UI fix — scrollbar hidden when editor is empty | ✅ done |

**Next:** `bmad-continue` → S5-01 (chat bubble).

---

## Completed Sprints ✅

| Sprint | Name | Stories |
|--------|------|---------|
| S1 | MVP Foundation | 7 done |
| S2 | LLM Normalization | 7 done |
| S3 | Polish & Production Readiness | 4 done |
| S4 | Advanced & Release Candidate | 4 done |

## Features Shipped (v1.0)

- Single-instance WinUI 3 notepad (Fluent / Win11)
- Auto-save (2s debounce) + versioned backups
- Markdown rendering (Markdig) + inline diff preview
- LLM normalization: Ollama, OpenAI, Anthropic
- Auto-detected providers via env vars: Mistral, Groq, Together, OpenRouter, Cohere
- OpenAI-compatible endpoint support
- DPAPI-protected API key storage
- FileSystemWatcher conflict detection
- Custom rules editor + MEMORY.md tracker
- 44 tests green → 63 tests green

## Build

```
dotnet build -r win-x64
dotnet test
```
