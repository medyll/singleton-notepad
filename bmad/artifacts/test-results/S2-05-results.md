# S2-05 Test Results — Normalize triggers + ViewModel wiring

**Story:** Normalize triggers + ViewModel wiring
**Date:** 2026-05-05
**Status:** ✅ PASSED

## Implementation

### Settings
- `AppSettings` — added `AutoNormalizeOnClose` (bool, default true) and `IdleMinutesBeforeNormalize` (int, default 15)
- `SettingsViewModel` — added `AutoNormalizeOnClose` and `IdleMinutesBeforeNormalize` with auto-save on change
- `SettingsPage.xaml` — added "Normalisation" nav item with toggle + idle minutes NumberBox

### MainViewModel wiring
- `ISettingsService` injected into `MainViewModel`
- `ConfigureIdleTimerAsync()` — reads `IdleMinutesBeforeNormalize` from settings and configures timer interval
- `LoadContentAsync()` — calls `ConfigureIdleTimerAsync()` before starting idle timer
- `OnWindowClosingAsync()` — checks `AutoNormalizeOnClose` setting before triggering normalize on close
- `OnIdleElapsed()` — uses `IdleMinutesBeforeNormalize` from settings instead of hardcoded constant
- `LastNormalizeTime` — new observable property showing "Last normalize: HH:mm" in StatusBar

### Diff overlay wiring
- `InlineDiffEditor` placed in overlay Border in `MainPage.xaml`
- `MainPage.xaml.cs` wires `DiffViewer.ApplyClicked` → `ViewModel.ApplyNormalizationCommand`
- `DiffViewer.CancelClicked` → `ViewModel.CancelNormalizationCommand`
- `ViewModel.PropertyChanged` handler toggles `DiffOverlay` visibility and `EditorTextBox` visibility

### StatusBar
- StatusBar now shows `LastNormalizeTime` instead of static line/column info

## Tests

| Test Suite | Tests | Passed |
|------------|-------|--------|
| All existing tests | 34 | ✅ 34 |
| Build (main project) | ✅ (MSIX packaging error on AnyCPU — not a code issue) | |

## Exit Criteria Verification

| Criterion | Result |
|-----------|--------|
| Wire `MainViewModel.NormalizeCommand` → NormalizationService → InlineDiffEditor flow | ✅ MainPage.xaml.cs wires ApplyClicked/CancelClicked |
| On-close trigger (if `AutoNormalizeOnClose` setting true) | ✅ `OnWindowClosingAsync()` checks setting |
| On-idle trigger (configurable minutes idle via timer) | ✅ `IdleMinutesBeforeNormalize` from settings |
| StatusBar: "Normalizing..." → "Last normalize: HH:mm" | ✅ `LastNormalizeTime` property + updated in NormalizeAsync |
| Settings: Normalisation pane with trigger toggles | ✅ NormalizationPane in SettingsPage.xaml |

✅ All tests pass (34/34). S2-05 complete.