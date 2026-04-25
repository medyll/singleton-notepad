namespace SingletonNotepad.Core.Models;

/// <summary>
/// Application settings model.
/// </summary>
public class AppSettings
{
    public string SingletonFilePath { get; set; } = string.Empty;
    public string RulesFilePath { get; set; } = string.Empty;
    public int AutoSaveDebounceMs { get; set; } = 2000;
    public bool AutoSaveEnabled { get; set; } = true;
    public bool AutoNormalizeOnClose { get; set; }
    public int AutoNormalizeIdleMinutes { get; set; }
    public int MinAutoNormalizeIntervalMinutes { get; set; } = 5;
    public string ActiveLlmProvider { get; set; } = "Ollama";
    public double WindowLeft { get; set; }
    public double WindowTop { get; set; }
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public string Theme { get; set; } = "System"; // "Light", "Dark", "System"
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = false;
    public bool HighlightMatchingBrackets { get; set; } = true;
    public bool CreateBackups { get; set; } = true;
}
