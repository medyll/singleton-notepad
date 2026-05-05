using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

/// <summary>
/// Main editor ViewModel. Manages editor content, sync state, and normalize command.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private string _syncState = "Ready";

    [ObservableProperty]
    private bool _isNormalizing;

    public MainViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _fileService.FileSaved += OnFileSaved;
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        EditorContent = await _fileService.LoadAsync(ct);
    }

    partial void OnEditorContentChanged(string value)
    {
        SyncState = "Saving...";
        _fileService.QueueAutoSave(value);
    }

    private void OnFileSaved()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            SyncState = "Sync \u2713";
        });
    }

    [RelayCommand]
    private async Task NormalizeAsync()
    {
        IsNormalizing = false;
    }

    [RelayCommand]
    private async Task CopyContentAsync()
    {
    }
}
