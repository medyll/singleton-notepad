using CommunityToolkit.Mvvm.ComponentModel;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

/// <summary>
/// Settings ViewModel. Manages all settings panes.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string _notesFilePath = string.Empty;

    [ObservableProperty]
    private bool _autoSave = true;

    [ObservableProperty]
    private int _autoSaveDelayMs = 2000;

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
    }

    public async Task SaveSettingsAsync(CancellationToken ct = default)
    {
        var settings = new AppSettings
        {
            Theme = Theme,
            NotesFilePath = NotesFilePath,
            AutoSave = AutoSave,
            AutoSaveDelayMs = AutoSaveDelayMs,
        };
        await _settingsService.SaveAsync(settings, ct);
    }

    partial void OnThemeChanged(string value) => _ = SaveSettingsAsync();
    partial void OnNotesFilePathChanged(string value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAutoSaveDelayMsChanged(int value) => _ = SaveSettingsAsync();
}
