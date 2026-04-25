namespace SingletonNotepad.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SingletonNotepad.Core.Services;

/// <summary>
/// Main ViewModel for the editor view.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly INormalizationService _normalizationService;
    private readonly IMemoryTrackerService _memoryTrackerService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    private bool _loading;

    [ObservableProperty]
    private SyncState _syncState = SyncState.Saved;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public MainViewModel(
        IFileService fileService,
        ISettingsService settingsService,
        INormalizationService normalizationService,
        IMemoryTrackerService memoryTrackerService,
        INotificationService notificationService)
    {
        _fileService = fileService;
        _settingsService = settingsService;
        _normalizationService = normalizationService;
        _memoryTrackerService = memoryTrackerService;
        _notificationService = notificationService;

        _fileService.FileSaved += OnFileSaved;
        _fileService.SaveFailed += OnSaveFailed;
        
        // Initialize file path from settings
        var settings = _settingsService.GetSettings();
        _fileService.FilePath = settings.SingletonFilePath;
    }

    partial void OnEditorContentChanged(string value)
    {
        if (_loading) return;
        SyncState = SyncState.Unsaved;
        _fileService.ScheduleSave(value);
    }

    private void OnFileSaved()
    {
        SyncState = SyncState.Saved;
        StatusMessage = "Sync ✓";
    }

    private void OnSaveFailed(Exception ex)
    {
        SyncState = SyncState.Saved;
        _notificationService.ShowError($"Auto-save failed: {ex.Message}");
    }

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        try
        {
            var content = await _fileService.LoadAsync();
            _loading = true;
            EditorContent = content;
            _loading = false;
            SyncState = SyncState.Saved;
            StatusMessage = "File loaded";
        }
        catch (Exception ex)
        {
            SyncState = SyncState.Saved;
            _notificationService.ShowError($"Failed to load file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        try
        {
            SyncState = SyncState.Saving;
            await _fileService.SaveAsync(EditorContent);
            SyncState = SyncState.Saved;
            StatusMessage = "Saved";
        }
        catch (Exception ex)
        {
            SyncState = SyncState.Saved;
            _notificationService.ShowError($"Failed to save: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NormalizeAsync()
    {
        try
        {
            _notificationService.Show("Normalizing...");
            var settings = _settingsService.GetSettings();
            var rulesPath = settings.RulesFilePath;
            var result = await _normalizationService.NormalizeAsync(EditorContent, rulesPath);
            
            if (result.HasChanges)
            {
                // TODO: Show diff preview in Sprint 2
                EditorContent = result.NormalizedContent;
                await SaveFileAsync();
                await _memoryTrackerService.AppendAsync(new Core.Models.ChangeRecord
                {
                    Timestamp = DateTime.Now,
                    Type = "normalize",
                    LinesChanged = 0, // Calculate in Sprint 2
                    Preview = EditorContent.Substring(0, Math.Min(100, EditorContent.Length))
                });
            }
            else
            {
                _notificationService.Show("No changes needed");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Normalization failed: {ex.Message}");
        }
    }
}

public enum SyncState
{
    Saved,
    Unsaved,
    Saving
}
