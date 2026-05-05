using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

/// <summary>
/// Persists and retrieves application settings as JSON.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from disk. Returns defaults if file doesn't exist.
    /// </summary>
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
