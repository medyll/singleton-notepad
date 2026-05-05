# S3-01 — Markdown Syntax Highlighting (Piste A: Toggle Preview)

**Date:** 2026-05-05
**Story:** S3-01
**Status:** done

## Summary

Implemented a toggle-based Markdown preview using `RichTextBlock` with Markdig rendering. User clicks the "Couleur" toggle in CommandBar to switch between plain TextBox editing and formatted Markdown preview.

## Changes

- **Added** `Views/Controls/MarkdownPreview.xaml(.cs)` — WinUI UserControl wrapping RichTextBlock, renders Markdown via Markdig with syntax colors:
  - Headings: blue bold (H1=24px bold, H2=20px semi-bold, H3=16px semi-bold)
  - Code inline: gray background, italic
  - Code blocks: gray background
  - Links: purple
  - Lists: gray bullet markers
  - Blockquotes: italic gray
- **Modified** `MainPage.xaml` — Added `AppBarToggleButton` "Couleur" with `IsSyntaxHighlightEnabled` binding; Editor Grid now contains both TextBox and MarkdownPreview
- **Modified** `MainPage.xaml.cs` — Toggle event handlers + `UpdateEditorVisibility()` method managing visibility of TextBox/MarkdownPreview/DiffOverlay
- **Modified** `MainViewModel.cs` — Added `IsSyntaxHighlightEnabled` [ObservableProperty] boolean
- **Added** `Markdig 0.38.0` package reference

## Acceptance Criteria

- [x] Toggle button in CommandBar switches between plain TextBox and formatted Markdown preview
- [x] Syntax colors: headings (blue), code (gray bg), links (purple), lists (gray bullets)
- [x] Toggle state stored in ViewModel, survives content changes
- [x] When diff overlay is shown, both editor and preview are hidden
- [x] All 34 existing tests still pass

## Test Results

```
dotnet test SingletonNotepad.Tests --verbosity quiet
Réussi! — échec : 0, réussite : 34, ignorée(s) : 0, total : 34
Duration: 1m
```

## Build

```
dotnet build SingletonNotepad.csproj -p:Platform=x64
SingletonNotepad -> bin\x64\Debug\net10.0-windows10.0.26100.0\SingletonNotepad.dll
La génération a réussi.
0 Avertissement(s) 0 Erreur(s)
```

## Notes

- This is Piste A (toggle preview) — not real-time inline highlighting. FR-12 from PRD says "Phase 3" for syntax highlighting. This implementation satisfies the requirement as a preview mode.
- Piste B (inline real-time) was rejected due to WinUI 3 TextBox limitations.