namespace SingletonNotepad.Core.Services;

using System.Text.Json;
using Windows.Security.Credentials;
using Windows.Storage;
using SingletonNotepad.Core.Models;

/// <summary>
/// Implementation of ISettingsService using LocalSettings and PasswordVault.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ApplicationDataContainer _localSettings;
    private const string ApiKeyResourceName = "SingletonNotepad.ApiKeys";
    private const string AppSettingsKey = "AppSettings";

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    /// <inheritdoc />
    public AppSettings GetSettings()
    {
        if (_localSettings.Values.TryGetValue(AppSettingsKey, out var value) && value is string json)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    // Migrate legacy window settings if present
                    MigrateLegacyWindowSettings(settings);
                    return settings;
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to deserialize settings: {ex.Message}");
            }
        }

        // Return defaults if no settings exist or deserialization failed
        return CreateDefaultSettings();
    }

    /// <inheritdoc />
    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        _localSettings.Values[AppSettingsKey] = json;
        
        // Also save window geometry separately for backward compatibility
        _localSettings.Values["WindowLeft"] = settings.WindowLeft;
        _localSettings.Values["WindowTop"] = settings.WindowTop;
        _localSettings.Values["WindowWidth"] = settings.WindowWidth;
        _localSettings.Values["WindowHeight"] = settings.WindowHeight;
    }

    /// <inheritdoc />
    public void ResetToDefaults()
    {
        _localSettings.Values.Remove(AppSettingsKey);
        
        // Also clear legacy keys
        _localSettings.Values.Remove("WindowLeft");
        _localSettings.Values.Remove("WindowTop");
        _localSettings.Values.Remove("WindowWidth");
        _localSettings.Values.Remove("WindowHeight");
        _localSettings.Values.Remove("SingletonFilePath");
        _localSettings.Values.Remove("RulesFilePath");
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        if (_localSettings.Values.TryGetValue(key, out var value))
        {
            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        _localSettings.Values[key] = value;
    }

    /// <inheritdoc />
    public string? GetApiKey(string provider)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(ApiKeyResourceName, provider);
            return credential.Password;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            // Key not found or vault error - log for debugging
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to retrieve API key: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public void SetApiKey(string provider, string key)
    {
        var vault = new PasswordVault();
        
        try
        {
            // Remove existing credential if present
            var existing = vault.Retrieve(ApiKeyResourceName, provider);
            vault.Remove(existing);
        }
        catch
        {
            // Ignore if doesn't exist
        }

        // Add new credential
        vault.Add(new PasswordCredential(ApiKeyResourceName, provider, key));
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

    /// <summary>
    /// Migrates legacy individual window settings to the AppSettings model.
    /// </summary>
    private void MigrateLegacyWindowSettings(AppSettings settings)
    {
        // If window settings exist as individual keys, use them
        if (_localSettings.Values.TryGetValue("WindowLeft", out var left))
            settings.WindowLeft = (double)left;
        if (_localSettings.Values.TryGetValue("WindowTop", out var top))
            settings.WindowTop = (double)top;
        if (_localSettings.Values.TryGetValue("WindowWidth", out var width))
            settings.WindowWidth = (double)width;
        if (_localSettings.Values.TryGetValue("WindowHeight", out var height))
            settings.WindowHeight = (double)height;
    }
}
