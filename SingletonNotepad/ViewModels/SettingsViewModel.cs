using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly INormalizationService _normalizationService;
    private readonly ILlmProviderSelector _providerSelector;

    private static readonly HashSet<string> BuiltInProviders = ["Ollama", "OpenAI", "Anthropic"];

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
    public partial string LlmProvider { get; set; } = "Ollama";

    [ObservableProperty]
    public partial string OllamaEndpoint { get; set; } = "http://localhost:11434";

    [ObservableProperty]
    public partial string OllamaModel { get; set; } = "qwen3.5:latest";

    [ObservableProperty]
    public partial string OpenAiModel { get; set; } = "gpt-4o-mini";

    [ObservableProperty]
    public partial string AnthropicModel { get; set; } = "claude-haiku-4-5-20251001";

    [ObservableProperty]
    public partial string OpenAiApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AnthropicApiKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int MaxBackupCount { get; set; } = 10;

    [ObservableProperty]
    public partial bool AlwaysStartAtBottom { get; set; } = true;

    [ObservableProperty]
    public partial bool AlwaysOnTop { get; set; } = false;

    [ObservableProperty]
    public partial ObservableCollection<BackupDisplayItem> Backups { get; set; } = new();

    [ObservableProperty]
    public partial string RulesContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableProviders { get; set; } = new();

    [ObservableProperty]
    public partial string CustomProviderModel { get; set; } = string.Empty;

    public Visibility OllamaSectionVisibility    => LlmProvider == "Ollama"    ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OpenAiSectionVisibility    => LlmProvider == "OpenAI"    ? Visibility.Visible : Visibility.Collapsed;
    public Visibility AnthropicSectionVisibility => LlmProvider == "Anthropic" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CustomProviderSectionVisibility => !BuiltInProviders.Contains(LlmProvider) ? Visibility.Visible : Visibility.Collapsed;

    public SettingsViewModel(ISettingsService settingsService, INormalizationService normalizationService, ILlmProviderSelector providerSelector)
    {
        _settingsService = settingsService;
        _normalizationService = normalizationService;
        _providerSelector = providerSelector;

        foreach (var name in _providerSelector.AvailableProviders)
            AvailableProviders.Add(name);
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
        LlmProvider = settings.LlmProvider;
        OllamaEndpoint = settings.OllamaEndpoint;
        OllamaModel = settings.OllamaModel;
        OpenAiModel = settings.OpenAiModel;
        AnthropicModel = settings.AnthropicModel;
        AlwaysStartAtBottom = settings.AlwaysStartAtBottom;
        AlwaysOnTop = settings.AlwaysOnTop;

        if (!string.IsNullOrEmpty(settings.OpenAiApiKey))
        {
            OpenAiApiKey = await _settingsService.UnprotectApiKeyAsync(settings.OpenAiApiKey, ct);
        }
        if (!string.IsNullOrEmpty(settings.AnthropicApiKey))
        {
            AnthropicApiKey = await _settingsService.UnprotectApiKeyAsync(settings.AnthropicApiKey, ct);
        }

        if (!BuiltInProviders.Contains(LlmProvider))
        {
            settings.ProviderModels.TryGetValue(LlmProvider, out var customModel);
            CustomProviderModel = customModel ?? string.Empty;
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
        settings.LlmProvider = LlmProvider;
        settings.OllamaEndpoint = OllamaEndpoint;
        settings.OllamaModel = OllamaModel;
        settings.OpenAiModel = OpenAiModel;
        settings.AnthropicModel = AnthropicModel;
        settings.AlwaysStartAtBottom = AlwaysStartAtBottom;
        settings.AlwaysOnTop = AlwaysOnTop;

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

        if (!BuiltInProviders.Contains(LlmProvider) && !string.IsNullOrEmpty(CustomProviderModel))
        {
            settings.ProviderModels[LlmProvider] = CustomProviderModel;
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

    partial void OnThemeChanged(string value)
    {
        App.ApplyTheme(value);
        _ = SaveSettingsAsync();
    }
    partial void OnNotesFilePathChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveDelayMsChanged(int value) => _ = SaveSettingsAsync();
    partial void OnAutoNormalizeOnCloseChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnIdleMinutesBeforeNormalizeChanged(int value) => _ = SaveSettingsAsync();
    partial void OnLlmProviderChanged(string value)
    {
        OnPropertyChanged(nameof(OllamaSectionVisibility));
        OnPropertyChanged(nameof(OpenAiSectionVisibility));
        OnPropertyChanged(nameof(AnthropicSectionVisibility));
        OnPropertyChanged(nameof(CustomProviderSectionVisibility));
        _ = LoadCustomProviderModelAsync();
        _ = SaveSettingsAsync();
    }

    private async Task LoadCustomProviderModelAsync()
    {
        if (BuiltInProviders.Contains(LlmProvider)) return;
        var settings = await _settingsService.LoadAsync();
        settings.ProviderModels.TryGetValue(LlmProvider, out var m);
        CustomProviderModel = m ?? ProviderDetectionService.GetDefaultModel(LlmProvider);
    }
    partial void OnOllamaEndpointChanged(string value) => _ = SaveSettingsAsync();
    partial void OnOllamaModelChanged(string value) => _ = SaveSettingsAsync();
    partial void OnOpenAiModelChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAnthropicModelChanged(string value) => _ = SaveSettingsAsync();
    partial void OnOpenAiApiKeyChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAnthropicApiKeyChanged(string value) => _ = SaveSettingsAsync();
    partial void OnCustomProviderModelChanged(string value) => _ = SaveSettingsAsync();
    partial void OnMaxBackupCountChanged(int value) => _ = SaveSettingsAsync();
    partial void OnAlwaysStartAtBottomChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAlwaysOnTopChanged(bool value)
    {
        if (App.Window?.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
            p.IsAlwaysOnTop = value;
        _ = SaveSettingsAsync();
    }
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
