namespace SingletonNotepad.Core.Models;

/// <summary>
/// Represents a change record in the memory tracking file.
/// </summary>
public class ChangeRecord
{
    public DateTime Timestamp { get; init; }
    public string Type { get; init; } = string.Empty; // "normalize", "edit", etc.
    public int LinesChanged { get; init; }
    public string Preview { get; init; } = string.Empty;
    public string? BackupReference { get; init; }
}
