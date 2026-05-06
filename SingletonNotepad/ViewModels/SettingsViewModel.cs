using CommunityToolkit.Mvvm.ComponentModel;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    public partial string Theme { get; set; } = "System";

    [ObservableProperty]
    public partial string NotesFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AutoSave { get; set; } = true;

    [ObservableProperty]
    public partial int AutoSaveDelayMs { get; set; } = 2000;

    [ObservableProperty]
    public partial bool AutoNormalizeOnClose { get; set; } = true;

    [ObservableProperty]
    public partial int IdleMinutesBeforeNormalize { get; set; } = 15;

    [ObservableProperty]
    public partial string OpenAiApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AnthropicApiKey { get; set; } = string.Empty;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        Theme = settings.Theme;
        NotesFilePath = settings.NotesFilePath;
        AutoSave = settings.AutoSave;
        AutoSaveDelayMs = settings.AutoSaveDelayMs;
        AutoNormalizeOnClose = settings.AutoNormalizeOnClose;
        IdleMinutesBeforeNormalize = settings.IdleMinutesBeforeNormalize;

        if (!string.IsNullOrEmpty(settings.OpenAiApiKey))
        {
            OpenAiApiKey = await _settingsService.UnprotectApiKeyAsync(settings.OpenAiApiKey, ct);
        }
        if (!string.IsNullOrEmpty(settings.AnthropicApiKey))
        {
            AnthropicApiKey = await _settingsService.UnprotectApiKeyAsync(settings.AnthropicApiKey, ct);
        }
    }

    public async Task SaveSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        settings.Theme = Theme;
        settings.NotesFilePath = NotesFilePath;
        settings.AutoSave = AutoSave;
        settings.AutoSaveDelayMs = AutoSaveDelayMs;
        settings.AutoNormalizeOnClose = AutoNormalizeOnClose;
        settings.IdleMinutesBeforeNormalize = IdleMinutesBeforeNormalize;

        if (!string.IsNullOrEmpty(OpenAiApiKey))
        {
            settings.OpenAiApiKey = await _settingsService.ProtectApiKeyAsync(OpenAiApiKey, ct);
        }
        else
        {
            settings.OpenAiApiKey = null;
        }

        if (!string.IsNullOrEmpty(AnthropicApiKey))
        {
            settings.AnthropicApiKey = await _settingsService.ProtectApiKeyAsync(AnthropicApiKey, ct);
        }
        else
        {
            settings.AnthropicApiKey = null;
        }

        await _settingsService.SaveAsync(settings, ct);
    }

    partial void OnThemeChanged(string value) => _ = SaveSettingsAsync();
    partial void OnNotesFilePathChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveDelayMsChanged(int value) => _ = SaveSettingsAsync();
    partial void OnAutoNormalizeOnCloseChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnIdleMinutesBeforeNormalizeChanged(int value) => _ = SaveSettingsAsync();
    partial void OnOpenAiApiKeyChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAnthropicApiKeyChanged(string value) => _ = SaveSettingsAsync();
}
