namespace SingletonNotepad.Core.Services;

using System.IO;
using System.Text.Json;
using Avalonia.Threading;
using SingletonNotepad.Core.Models;

/// <summary>
/// Implementation of ISettingsService using JSON file storage (cross-platform).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly string _secureStoragePath;
    private AppSettings? _cachedSettings;
    private Dictionary<string, string> _apiKeys = new();

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SingletonNotepad");

        Directory.CreateDirectory(appDataPath);

        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _secureStoragePath = Path.Combine(appDataPath, "apikeys.json");

        LoadApiKeys();
    }

    /// <inheritdoc />
    public AppSettings GetSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _cachedSettings = settings;
                    return settings;
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to deserialize settings: {ex.Message}");
            }
        }

        // Return defaults if no settings exist or deserialization failed
        _cachedSettings = CreateDefaultSettings();
        return _cachedSettings;
    }

    /// <inheritdoc />
    public void SaveSettings(AppSettings settings)
    {
        _cachedSettings = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        if (File.Exists(_settingsPath))
            File.Delete(_settingsPath);

        _cachedSettings = null;
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        var settings = GetSettings();

        return key switch
        {
            "Theme" => (T)(object)(settings.Theme ?? defaultValue.ToString()!),
            "AutoSaveDebounceMs" => (T)(object)settings.AutoSaveDebounceMs,
            "WindowWidth" => (T)(object)settings.WindowWidth,
            "WindowHeight" => (T)(object)settings.WindowHeight,
            _ => defaultValue
        };
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        var settings = GetSettings();

        switch (key)
        {
            case "Theme":
                settings.Theme = value.ToString();
                break;
            case "AutoSaveDebounceMs":
                settings.AutoSaveDebounceMs = (int)(object)value;
                break;
            case "WindowWidth":
                settings.WindowWidth = (double)(object)value;
                break;
            case "WindowHeight":
                settings.WindowHeight = (double)(object)value;
                break;
        }

        SaveSettings(settings);
    }

    /// <inheritdoc />
    public string? GetApiKey(string provider)
    {
        return _apiKeys.TryGetValue(provider, out var key) ? key : null;
    }

    /// <inheritdoc />
    public void SetApiKey(string provider, string key)
    {
        _apiKeys[provider] = key;
        SaveApiKeys();
    }

    private void LoadApiKeys()
    {
        if (File.Exists(_secureStoragePath))
        {
            try
            {
                var json = File.ReadAllText(_secureStoragePath);
                _apiKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch
            {
                _apiKeys = new();
            }
        }
    }

    private void SaveApiKeys()
    {
        var json = JsonSerializer.Serialize(_apiKeys, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_secureStoragePath, json);
    }

    /// <summary>
    /// Creates default settings with sensible initial values.
    /// </summary>
    private AppSettings CreateDefaultSettings()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return new AppSettings
        {
            SingletonFilePath = Path.Combine(documentsPath, "MY_SINGLETON_NOTEPAD.md"),
            RulesFilePath = Path.Combine(documentsPath, "NOTEPAD_SINGLETON_AGENTS.md"),
            Theme = "System",
            AutoSaveDebounceMs = 2000,
            AutoNormalizeOnClose = false,
            AutoNormalizeIdleMinutes = 0,
            MinAutoNormalizeIntervalMinutes = 5,
            ActiveLlmProvider = "Ollama",
            WindowWidth = 1200,
            WindowHeight = 800,
            WindowLeft = 0,
            WindowTop = 0
        };
    }
}
