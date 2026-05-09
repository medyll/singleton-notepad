using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsPath;
    private AppSettings? _cache;

    public SettingsService(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _settingsPath = Path.Combine(customPath, "settings.json");
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsDir = Path.Combine(localAppData, "SingletonNotepad");
            _settingsPath = Path.Combine(settingsDir, "settings.json");
        }
    }

    public static string GetDefaultSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(localAppData, "SingletonNotepad");
        return Path.Combine(settingsDir, "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_settingsPath))
        {
            _cache = new AppSettings();
            return _cache;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath, ct);
            _cache = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return _cache;
        }
        catch (JsonException)
        {
            _cache = new AppSettings();
            return _cache;
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        _cache = settings;
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json, ct);
    }

    public Task<string> ProtectApiKeyAsync(string plainText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(plainText))
            return Task.FromResult(string.Empty);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Task.FromResult(Convert.ToBase64String(protectedBytes));
    }

    public Task<string> UnprotectApiKeyAsync(string protectedText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(protectedText))
            return Task.FromResult(string.Empty);

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedText);
            var plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Task.FromResult(Encoding.UTF8.GetString(plainBytes));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(string.Empty);
        }
        catch (FormatException)
        {
            return Task.FromResult(string.Empty);
        }
    }
}