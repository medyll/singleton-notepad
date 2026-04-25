namespace SingletonNotepad.Core.Models;

/// <summary>
/// Result of a normalization operation.
/// </summary>
public class NormalizationResult
{
    public string OriginalContent { get; init; } = string.Empty;
    public string? NormalizedContent { get; init; }
    public bool HasChanges { get; init; }
    public string? DiffHtml { get; init; }
    public TimeSpan Elapsed { get; init; }
}
