using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly INormalizationService _normalizationService;

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

    [ObservableProperty]
    public partial int MaxBackupCount { get; set; } = 10;

    [ObservableProperty]
    public partial ObservableCollection<BackupDisplayItem> Backups { get; set; } = new();

    [ObservableProperty]
    public partial string RulesContent { get; set; } = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, INormalizationService normalizationService)
    {
        _settingsService = settingsService;
        _normalizationService = normalizationService;
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
        MaxBackupCount = settings.MaxBackupCount ?? 10;

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
        settings.MaxBackupCount = MaxBackupCount;

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

    public async Task LoadBackupsAsync(CancellationToken ct = default)
    {
        var backups = await _normalizationService.GetBackupsAsync(ct);
        Backups.Clear();
        foreach (var b in backups)
        {
            Backups.Add(new BackupDisplayItem(b));
        }
    }

    public async Task RestoreBackupAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return;
        var content = await File.ReadAllTextAsync(path, ct);
        var fileService = App.Services.GetService(typeof(IFileService)) as IFileService;
        if (fileService != null)
        {
            await fileService.SaveAsync(content, ct);
        }
    }

    public async Task DeleteBackupAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return;
        File.Delete(path);
        await LoadBackupsAsync(ct);
    }

    public async Task LoadRulesAsync(CancellationToken ct = default)
    {
        var rule = await _normalizationService.LoadRulesAsync(ct);
        RulesContent = rule.Content;
    }

    public async Task SaveRulesAsync(CancellationToken ct = default)
    {
        await File.WriteAllTextAsync(_normalizationService.RulesFilePath, RulesContent, ct);
    }

    partial void OnThemeChanged(string value) => _ = SaveSettingsAsync();
    partial void OnNotesFilePathChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveDelayMsChanged(int value) => _ = SaveSettingsAsync();
    partial void OnAutoNormalizeOnCloseChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnIdleMinutesBeforeNormalizeChanged(int value) => _ = SaveSettingsAsync();
    partial void OnOpenAiApiKeyChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAnthropicApiKeyChanged(string value) => _ = SaveSettingsAsync();
    partial void OnMaxBackupCountChanged(int value) => _ = SaveSettingsAsync();
}

public class BackupDisplayItem
{
    private readonly BackupInfo _info;

    public BackupDisplayItem(BackupInfo info) => _info = info;

    public string Path => _info.Path;
    public string VersionDisplay => $"v{_info.Version}";
    public string TimestampDisplay => _info.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    public string SizeDisplay => _info.SizeBytes < 1024
        ? $"{_info.SizeBytes} B"
        : $"{_info.SizeBytes / 1024.0:F1} KB";
}
