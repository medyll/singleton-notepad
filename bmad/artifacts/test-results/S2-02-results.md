# Test Results — S2-02: NormalizationService

**Date:** 2026-05-05
**Developer:** Riley
**Command:** `dotnet test SingletonNotepad.Tests -c Debug -p:RuntimeIdentifier=win-x64`

## Results

**Total: 27 tests — 27 passed, 0 failed**

✅ All tests passed

## S2-02 New Tests (11)

| Test | Status |
|------|--------|
| LoadRulesAsync_ReturnsEmpty_WhenFileNotExists | ✅ |
| LoadRulesAsync_ReturnsContent_WhenFileExists | ✅ |
| IsRateLimited_ReturnsFalse_OnFirstCall | ✅ |
| IsRateLimited_ReturnsTrue_WhenContentUnchanged | ✅ |
| IsRateLimited_ReturnsFalse_WhenContentChanged | ✅ |
| ExceedsSizeLimit_ReturnsFalse_ForNormalContent | ✅ |
| ExceedsSizeLimit_ReturnsTrue_ForLargeContent | ✅ |
| ExceedsSizeLimit_ReturnsTrue_ForManyLines | ✅ |
| NormalizeAsync_ReturnsResultWithDiff | ✅ |
| NormalizeAsync_BackupFileCreated | ✅ |
| NormalizeAsync_CallsLlmWithPromptContainingRules | ✅ |

## Coverage

- Rules loading (empty + content)
- Rate-limit guard (first call, unchanged content, changed content)
- Size limits (byte count, line count)
- Full normalization pipeline (prompt building, LLM call, diff computation, backup)
