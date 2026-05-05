# Sprint 2 — LLM Normalization

**Architect:** Ambre  
**Date:** 2026-05-05  
**Goal:** Deliver Ollama-powered normalization pipeline with inline diff preview.

---

## Stories

### S2-01 — ILlmProvider + OllamaProvider
- Create `ILlmProvider` with `Name` + `CompleteAsync(string prompt, CancellationToken ct)`
- Create `OllamaProvider` calling `POST /api/generate` (streaming off), parse response
- Register in DI as singleton keyed by provider name
- 30s HTTP timeout, error → user-visible toast

### S2-02 — NormalizationService
- `INormalizationService` / `NormalizationService`
- Load rules from `NOTEPAD_SINGLETON_AGENTS.md` (via IFileService)
- Build prompt: system rules + user content
- Rate-limit: 5 min since last normalize + file unchanged (SHA-256)
- Size guard: warn if >500KB or >10k lines
- Run `ILlmProvider`, compute DiffPlex diff, return `NormalizationResult`
- Backup content before apply

### S2-03 — MemoryTrackerService
- `IMemoryTrackerService` / `MemoryTrackerService`
- Append Markdown records to `NOTEPAD_SINGLETON_MEMORY.md`
- Record: `| YYYY-MM-DD HH:mm | Normalize | ±N lines | diff preview | backup path |`
- Called by ViewModel after Apply (not by NormalizationService)

### S2-04 — InlineDiffEditor control
- WinUI UserControl: `InlineDiffEditor.xaml(.cs)`
- DiffPlex side-by-side or inline word-diff rendering
- Colors: added (green bg), deleted (red strikethrough), modified (orange bg)
- Apply / Cancel buttons with event callbacks
- Lazy render for files >200 lines (viewport only)

### S2-05 — Normalize triggers + ViewModel wiring
- Wire `MainViewModel.NormalizeCommand` → NormalizationService → InlineDiffEditor flow
- On-close trigger (if `AutoNormalizeOnClose` setting true)
- On-idle trigger (configurable minutes idle via `System.Timers.Timer`)
- StatusBar: "Normalizing..." → "Last normalize: HH:mm"
- Settings: Normalisation pane with trigger toggles

### S2-06 — DefaultRules.md + AGENTS.md auto-create
- Create `Resources/DefaultRules.md` (seed content: Markdown structure rules)
- Auto-create `NOTEPAD_SINGLETON_AGENTS.md` if missing (copy from Resources)
- Integrate into NormalizationService startup

### S2-07 — Unit tests
- NormalizationService: mock ILlmProvider, test rules loading, rate-limit, size guard
- MemoryTrackerService: verify append-only format
- OllamaProvider: integration test against localhost (skip if unavailable)

---

## Dependencies to add
- `DiffPlex 1.9` — diff computation
- `Markdig 3.x` — Markdown parsing/validation

## Exit criteria
- User clicks [Normaliser] → preview diff → Apply → MEMORY.md updated
- All 7 stories done with passing tests
- No regression in Sprint 1 features
