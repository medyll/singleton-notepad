namespace SingletonNotepad.Core.Models;

public class AppSettings
{
    public string Theme { get; set; } = "System";
    public string NotesFilePath { get; set; } = string.Empty;
    public bool AutoSave { get; set; } = true;
    public int AutoSaveDelayMs { get; set; } = 2000;
}
