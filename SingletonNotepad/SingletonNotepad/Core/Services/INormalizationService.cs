namespace SingletonNotepad.Core.Services;

using SingletonNotepad.Core.Models;

/// <summary>
/// Service for LLM-driven content normalization.
/// </summary>
public interface INormalizationService
{
    /// <summary>
    /// Normalizes content using the active LLM provider and rules file.
    /// </summary>
    Task<NormalizationResult> NormalizeAsync(
        string content,
        string rulesPath,
        CancellationToken cancellationToken = default);
}
