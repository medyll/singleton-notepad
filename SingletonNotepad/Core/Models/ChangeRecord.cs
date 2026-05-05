namespace SingletonNotepad.Core.Models;

public class ChangeRecord
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty;
    public int LinesChanged { get; set; }
    public string Preview { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
}
