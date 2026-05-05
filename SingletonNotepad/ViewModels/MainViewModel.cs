using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    public partial string EditorContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SyncState { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsNormalizing { get; set; }

    public MainViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _fileService.FileSaved += OnFileSaved;
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        EditorContent = await _fileService.LoadAsync(ct);
        _fileService.Watch(OnExternalChange);
    }

    partial void OnEditorContentChanged(string value)
    {
        SyncState = "Saving...";
        _fileService.QueueAutoSave(value);
    }

    private void OnFileSaved()
    {
        _dispatcherQueue.TryEnqueue(() => SyncState = "Sync ✓");
    }

    private void OnExternalChange(string newContent)
    {
        _dispatcherQueue.TryEnqueue(() => EditorContent = newContent);
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        EditorContent = await _fileService.LoadAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _fileService.CancelAutoSave();
        await _fileService.SaveAsync(EditorContent);
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        EditorContent = await _fileService.LoadAsync();
        SyncState = "Reloaded";
    }

    [RelayCommand]
    private async Task NormalizeAsync()
    {
        IsNormalizing = true;
        try
        {
            // Stub: Sprint 2
            await Task.Delay(100);
        }
        finally
        {
            IsNormalizing = false;
        }
    }

    [RelayCommand]
    private void CopyContent()
    {
        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(EditorContent);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (App.Window is MainWindow mw)
        {
            mw.NavigateToPage(typeof(SingletonNotepad.Views.SettingsPage));
        }
    }
}
