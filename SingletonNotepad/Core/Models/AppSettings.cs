namespace SingletonNotepad.Core.Models;

/// <summary>
/// Application settings persisted to JSON.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Path to the singleton Markdown file.
    /// </summary>
    public string NotesFilePath { get; set; } = string.Empty;

    /// <summary>
    /// UI theme: "Light", "Dark", or "System".
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Whether auto-save is enabled.
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    /// Auto-save debounce delay in milliseconds.
    /// </summary>
    public int AutoSaveDelayMs { get; set; } = 2000;

    /// <summary>
    /// Window geometry (X, Y, Width, Height).
    /// </summary>
    public WindowGeometry WindowGeometry { get; set; } = new();

    /// <summary>
    /// Active LLM provider name.
    /// </summary>
    public string LlmProvider { get; set; } = "Ollama";

    /// <summary>
    /// Ollama endpoint URL.
    /// </summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Ollama model name.
    /// </summary>
    public string OllamaModel { get; set; } = "qwen3.5:latest";

    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    public string AnthropicModel { get; set; } = "claude-haiku-4-5-20251001";

    public string? OpenAiApiKey { get; set; }

    public string? AnthropicApiKey { get; set; }

    /// <summary>
    /// Auto-normalize on file close.
    /// </summary>
    public bool AutoNormalizeOnClose { get; set; } = true;

    /// <summary>
    /// Idle minutes before auto-normalization triggers.
    /// </summary>
    public int IdleMinutesBeforeNormalize { get; set; } = 15;

    /// <summary>
    /// Maximum number of versioned backups to keep.
    /// </summary>
    public int? MaxBackupCount { get; set; } = 10;

    /// <summary>
    /// Minimum seconds between two normalizations of identical content.
    /// </summary>
    public int NormalizeRateLimitSeconds { get; set; } = 10;

    /// <summary>
    /// Always position window at bottom-center on startup.
    /// </summary>
    public bool AlwaysStartAtBottom { get; set; } = true;

    /// <summary>
    /// Keep window above all other windows.
    /// </summary>
    public bool AlwaysOnTop { get; set; } = false;

    /// <summary>
    /// Path to the folder containing skill .md files.
    /// </summary>
    public string SkillsPath { get; set; } = string.Empty;

    /// <summary>
    /// Model overrides for env-detected providers (key = provider name, value = model string).
    /// </summary>
    public Dictionary<string, string> ProviderModels { get; set; } = new();

    /// <summary>
    /// Spell check enabled.
    /// </summary>
    public bool SpellCheckEnabled { get; set; } = true;

    /// <summary>
    /// Spell check language: "auto", "fr-FR", "en-US", etc.
    /// </summary>
    public string SpellCheckLanguage { get; set; } = "auto";

    /// <summary>
    /// Chat bubble visibility: "open" or "minimized".
    /// </summary>
    public string ChatBubbleState { get; set; } = "minimized";

    public string ChatLlmProvider { get; set; } = string.Empty;
}

/// <summary>
/// Window position and size.
/// </summary>
public class WindowGeometry
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
}
