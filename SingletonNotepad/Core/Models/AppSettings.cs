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
    public string OllamaModel { get; set; } = "llama3";

    /// <summary>
    /// OpenAI API key (plain JSON in Sprint 1, DPAPI in Sprint 3).
    /// </summary>
    public string? OpenAiApiKey { get; set; }

    /// <summary>
    /// Anthropic API key (plain JSON in Sprint 1, DPAPI in Sprint 3).
    /// </summary>
    public string? AnthropicApiKey { get; set; }
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
