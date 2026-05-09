namespace SingletonNotepad.Core.Models;

public class NormalizationResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string NormalizedContent { get; set; } = string.Empty;
    public DiffPayload DiffJson { get; set; } = new();
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public int LinesModified { get; set; }
    public bool HasChanges => LinesAdded > 0 || LinesDeleted > 0 || LinesModified > 0;
    public string BackupPath { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
