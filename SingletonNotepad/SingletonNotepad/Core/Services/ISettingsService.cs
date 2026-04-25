namespace SingletonNotepad.Core.Services;

/// <summary>
/// Service for application settings persistence.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value by key with a default fallback.
    /// </summary>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a setting value by key.
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Gets the API key for a specific LLM provider from secure storage.
    /// </summary>
    string? GetApiKey(string provider);

    /// <summary>
    /// Sets the API key for a specific LLM provider in secure storage.
    /// </summary>
    void SetApiKey(string provider, string key);
}
