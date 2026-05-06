using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
    Task<string> ProtectApiKeyAsync(string plainText, CancellationToken ct = default);
    Task<string> UnprotectApiKeyAsync(string protectedText, CancellationToken ct = default);
}
