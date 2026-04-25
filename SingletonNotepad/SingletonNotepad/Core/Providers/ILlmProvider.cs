namespace SingletonNotepad.Core.Providers;

/// <summary>
/// Strategy interface for LLM providers.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Gets the display name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Normalizes content using the provider's LLM.
    /// </summary>
    Task<string> NormalizeAsync(
        string rules,
        string content,
        CancellationToken cancellationToken = default);
}
