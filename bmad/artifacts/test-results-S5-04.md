# Test Results — S5-04

**Date:** 2026-05-08
**Story:** UI fix — scrollbar hidden when editor is empty

## Tests Added

| Test | Status |
|------|--------|
| Editor_UsesMinHeight_NotFixedHeight | ✅ Pass |
| Editor_OverflowY_IsAuto | ✅ Pass |
| Editor_NoScrollbarWhenEmpty_CombinedRules | ✅ Pass |
| ProseMirror_MinHeight_FillsEditor | ✅ Pass |

## All Tests

```
Réussi!  - échec: 0, réussite: 48, ignorée(s): 0, total: 48
```

## Change Summary

- `editor.html`: Changed `#editor` from `height: 100vh` to `min-height: 100vh`
- Scrollbar now only appears when content overflows
- No regression in scroll behavior with long content
