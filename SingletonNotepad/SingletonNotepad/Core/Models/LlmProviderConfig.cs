namespace SingletonNotepad.Core.Models;

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public class LlmProviderConfig
{
    public string ProviderName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
