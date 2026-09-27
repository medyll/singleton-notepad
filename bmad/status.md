# BMAD Status — singleton-notepad
_Last updated: 2026-08-23_

## Phase: development (Sprint 6)  |  Progress: 75%

---

## 🎯 Next Action
> S6-04 and S6-08 complete. Next: S6-05 (ChatBubble UX polish), then S6-06 (NormalizeRateLimitSeconds slider, quick win).

**Next command:** `bmad-continue`
**Next role:** Developer

---

## 🎯 Marketing
Skills auto-injection live. Normalization and chat silently leverage user-defined skill files. v1.0 UX groundwork in place — S6 fixes the remaining rough edges.

## 📦 Product
S6-01..S6-04 + S6-07 + S6-08 done. Inline normalization diff renders inside the TipTap editor via ProseMirror decorations. Chat has floating (FAB) and split modes with persisted mode + split height. **Build was broken at HEAD** (2026-08-23 audit): `GridSplitter` does not exist in WinUI 3 → XamlCompiler silent crash since commit e67e84c. Fixed with a custom drag splitter wired to `ChatViewModel.SplitHeight` (min 120, debounced persistence). 83/83 tests passing.

## 🔭 Far Vision
v1.0 = frictionless single-file notepad with trustworthy inline LLM normalization and an integrated chat that feels native, not bolted on.

---

## Audit & fixes — 2026-08-23

Status docs were stale vs git HEAD. Verified against code and corrected:

| Finding | Reality | Action |
|---------|---------|--------|
| S6-04 marked todo | Floating/split toggle, settings (`ChatBubbleMode`, `ChatSplitHeight`), split panel in MainPage all implemented (e67e84c) — but app did not compile (`GridSplitter` is not a WinUI 3 type) | Marked done; replaced GridSplitter with custom drag splitter + row-height binding + 500ms-debounced save |
| S6-08 marked todo | SkillService implemented and wired (ChatService, NormalizationService, App DI, SettingsViewModel, SkillServiceTests; commits b41154b, f9f0228) | Marked done |
| "tests passing" claims | 2/83 failing: `EditorHtmlTests` expected `#editor { min-height }`, editor.html had `height: 100%` | editor.html fixed → 83/83 |
| Tests project | Compiles Core sources via `<Compile Include>` links — does NOT validate XAML build | Now also validating `dotnet build SingletonNotepad.csproj -p:Platform=x64` |

Remaining S6 gaps: S6-05 (no loading ring, no user/AI message styling, clear-without-confirm, raw error text), S6-06 (rate-limit slider absent from SettingsPage; backend exists at MainViewModel.cs:176), split-chat UI duplicated between MainPage.xaml and ChatBubble.xaml.

---

## Sprints

| Sprint | Name | Status |
|--------|------|--------|
| S1 | MVP Foundation | ✅ complete |
| S2 | LLM Normalization | ✅ complete |
| S3 | Polish & Production Readiness | ✅ complete |
| S4 | Advanced & Release Candidate | ✅ complete |
| S5 | Chat & Model UX | ✅ complete |
| S6 | Editor Quality & UX Refactor | 🔄 in_progress |

### Sprint 6 stories

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| S6-01 | Inline diff — overlay approach | ✅ done | Superseded by S6-03 |
| S6-02 | Dead code sweep (InlineDiffEditor, MarkdownPreview, #diff-view) | ✅ done | MarkdownPreview + IsShowingDiff removed |
| S6-03 | True inline diff — ProseMirror decorations | ✅ done | Inline decorations with action bar |
| S6-04 | ChatBubble float/split layout redesign | ✅ done | Implemented in e67e84c; build broken by invalid `GridSplitter`, fixed 2026-08-23 (custom drag splitter + SplitHeight binding). Minor spec deviations: FAB bottom-center (spec: bottom-right), float panel 200px (spec: 320px) |
| S6-05 | ChatBubble UX polish (loading, roles, styling) | 📋 todo | Partial: Ctrl+Enter send exists; loading ring, role styling, clear confirm, InfoBar errors missing |
| S6-06 | Settings — NormalizeRateLimitSeconds UI | 📋 todo | Backend live (AppSettings + MainViewModel), slider missing |
| S6-07 | TipTap bundle audit + ProseMirror API exposition | ✅ done | Exports added, BUILDING.md in `_tiptap-build/` (not Assets/Editor/ as planned) |
| S6-08 | Skills integration | ✅ done | Auto-selection + manual `/skill` invoke in chat and normalization (b41154b, f9f0228) |

---

## Architecture decisions

### Diff approach (S6-03)
- **Rejected:** separate `#diff-view` div (overlay, replaces editor)
- **Rejected:** `editor.setContent(diffHtml)` with TipTap schema (strips unknown tags)
- **Chosen:** ProseMirror `Decoration` / `DecorationSet` applied to live editor
  - Editor stays visible and read-only during review
  - Decorations: inline marks for del/ins/mod at character positions
  - Insertions shown as ProseMirror widget decorations
  - Action bar: `position:sticky` inside editor container (not overlay sibling)

### Chat bubble (S6-04)
- **Float mode** (default): FAB + popup panel over editor, editor unaffected
- **Split mode**: MainPage Grid row split, custom drag splitter (GridSplitter does not exist in WinUI 3), persisted height (`AppSettings.ChatSplitHeight`, min 120, debounced save)
- Toggle button in CommandBar + split panel header
- Visibility: floating bubble collapsed when mode=split (`UpdateChatVisibility` in MainPage.xaml.cs)

### Technical debt (current)
- Split-chat UI duplicated: inline in `MainPage.xaml` AND in `Views/Controls/ChatBubble.xaml` — extract shared control
- Float panel fixed `Height="200"` — spec said 320px default, stretchable
- Cleared (S6-02): InlineDiffEditor, MarkdownPreview, IsShowingDiff, #diff-view

