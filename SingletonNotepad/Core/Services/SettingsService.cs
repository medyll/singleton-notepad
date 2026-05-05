using System.Text.Json;
using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

/// <summary>
/// Stub implementation — full implementation in S1-03.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        // Stub: return defaults. Full impl in S1-03.
        return Task.FromResult(new AppSettings());
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        // Stub: no-op. Full impl in S1-03.
        return Task.CompletedTask;
    }
}
