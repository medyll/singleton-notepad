using System.Collections.ObjectModel;
using DiffPlex.DiffBuilder.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SingletonNotepad.Views.Controls;

public sealed partial class InlineDiffEditor : UserControl
{
    private const int MaxRenderLines = 200;

    public event EventHandler? ApplyClicked;
    public event EventHandler? CancelClicked;

    public ObservableCollection<DiffLineViewModel> DiffLines { get; } = new();

    public InlineDiffEditor()
    {
        InitializeComponent();
        DiffItems.ItemsSource = DiffLines;
    }

    public void SetDiff(SideBySideDiffModel diff)
    {
        DiffLines.Clear();

        var oldLines = diff.OldText?.Lines ?? new List<DiffPiece>();
        var newLines = diff.NewText?.Lines ?? new List<DiffPiece>();

        var maxLines = Math.Max(oldLines.Count, newLines.Count);
        var renderLimit = Math.Min(maxLines, MaxRenderLines);

        for (int i = 0; i < renderLimit; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;

            AddLine(oldLine, newLine, i + 1);
        }

        if (maxLines > MaxRenderLines)
        {
            DiffLines.Add(new DiffLineViewModel
            {
                LineNumber = "...",
                Text = $"... {maxLines - MaxRenderLines} more lines (scroll to apply full content)",
                ChangeType = ChangeType.Imaginary,
            });
        }

        UpdateCounts(diff);
    }

    private void AddLine(DiffPiece? oldLine, DiffPiece? newLine, int lineNum)
    {
        var changeType = GetChangeType(oldLine, newLine);
        var text = newLine?.Text ?? oldLine?.Text ?? string.Empty;
        var lineNumber = newLine?.Position?.ToString() ?? oldLine?.Position?.ToString() ?? lineNum.ToString();

        DiffLines.Add(new DiffLineViewModel
        {
            LineNumber = lineNumber,
            Text = text,
            ChangeType = changeType,
        });
    }

    private static ChangeType GetChangeType(DiffPiece? oldLine, DiffPiece? newLine)
    {
        if (newLine?.Type == ChangeType.Inserted)
        {
            return ChangeType.Inserted;
        }

        if (oldLine?.Type == ChangeType.Deleted)
        {
            return ChangeType.Deleted;
        }

        if (oldLine?.Type == ChangeType.Modified || newLine?.Type == ChangeType.Modified)
        {
            return ChangeType.Modified;
        }

        return ChangeType.Unchanged;
    }

    private void UpdateCounts(SideBySideDiffModel diff)
    {
        int added = 0, deleted = 0, modified = 0;

        foreach (var line in diff.NewText?.Lines ?? new List<DiffPiece>())
        {
            if (line.Type == ChangeType.Inserted) added++;
            else if (line.Type == ChangeType.Modified) modified++;
        }

        foreach (var line in diff.OldText?.Lines ?? new List<DiffPiece>())
        {
            if (line.Type == ChangeType.Deleted) deleted++;
            else if (line.Type == ChangeType.Modified) modified++;
        }

        AddedCount.Text = $"+{added} added";
        DeletedCount.Text = $"-{deleted} deleted";
        ModifiedCount.Text = $"~{modified} modified";
    }

    private void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        ApplyClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
    }
}

public class DiffLineViewModel
{
    public string LineNumber { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }

    public SolidColorBrush BackgroundBrush => ChangeType switch
    {
        ChangeType.Inserted => new SolidColorBrush(Color.FromArgb(40, 16, 124, 16)),
        ChangeType.Deleted => new SolidColorBrush(Color.FromArgb(40, 209, 52, 56)),
        ChangeType.Modified => new SolidColorBrush(Color.FromArgb(40, 255, 140, 0)),
        _ => new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
    };
}
