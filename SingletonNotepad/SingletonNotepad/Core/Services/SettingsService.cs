namespace SingletonNotepad.Core.Services;

using Windows.Security.Credentials;
using Windows.Storage;

/// <summary>
/// Implementation of ISettingsService using LocalSettings and PasswordVault.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ApplicationDataContainer _localSettings;
    private const string ApiKeyResourceName = "SingletonNotepad.ApiKeys";

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_localSettings.Values.TryGetValue(key, out var value))
        {
            return (T)value;
        }
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _localSettings.Values[key] = value;
    }

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
}
