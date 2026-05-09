# BMAD Status — singleton-notepad
_Last updated: 2026-05-09_

## Phase: development (Sprint 6)  |  Progress: 60%

---

## 🎯 Next Action
> S6-07, S6-03, S6-02 complete. Next: S6-04 (ChatBubble float/split redesign).

**Next command:** `bmad-continue`
**Next role:** Developer

---

## 🎯 Marketing
Skills auto-injection live. Normalization and chat silently leverage user-defined skill files. v1.0 UX groundwork in place — S6 fixes the remaining rough edges.

## 📦 Product
S6-01 "done" but superseded: diff overlay still replaces editor visually. S6-03 is the real fix using ProseMirror decorations. Chat bubble has legibility and layout debt. Full S6 plan now written.

## 🔭 Far Vision
v1.0 = frictionless single-file notepad with trustworthy inline LLM normalization and an integrated chat that feels native, not bolted on.

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
| S6-04 | ChatBubble float/split layout redesign | 📋 todo | Major UX |
| S6-05 | ChatBubble UX polish (loading, roles, styling) | 📋 todo | After S6-04 |
| S6-06 | Settings — NormalizeRateLimitSeconds UI | 📋 todo | Quick |
| S6-07 | TipTap bundle audit + ProseMirror API exposition | ✅ done | Exports added, BUILDING.md created |
| S6-08 | Skills integration | 📋 todo | |

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
- **Split mode**: MainPage Grid row split, GridSplitter drag handle, persisted ratio
- Toggle button in CommandBar
- `AppSettings.ChatBubbleMode` + `AppSettings.ChatSplitHeight`

### Technical debt to clear
- `Views/Controls/InlineDiffEditor.xaml` — in csproj? Delete if so
- `Views/Controls/MarkdownPreview.xaml` — referenced anywhere? Delete if not
- `#diff-view` CSS+HTML — delete after S6-03
- `IsShowingDiff` ViewModel prop — remove after S6-03
