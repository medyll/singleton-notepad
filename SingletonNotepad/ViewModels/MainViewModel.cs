using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;
using Timer = System.Timers.Timer;

namespace SingletonNotepad.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly INormalizationService _normalizationService;
    private readonly IMemoryTrackerService _memoryTrackerService;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Timer _idleTimer;
    private DateTime _lastUserActivity;
    private volatile bool _isReloading;
    private string _lastSavedContent = string.Empty;
    private readonly object _saveLock = new();

    [ObservableProperty]
    public partial string EditorContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SyncState { get; set; } = "Ready";

    [ObservableProperty]
    public partial string NormalizeStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastNormalizeTime { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNormalizing { get; set; }

    [ObservableProperty]
    public partial bool IsSyntaxHighlightEnabled { get; set; }

    [ObservableProperty]
    public partial NormalizationResult? PendingNormalization { get; set; }

    [ObservableProperty]
    public partial bool HasExternalChange { get; set; }

    [ObservableProperty]
    public partial string SpellCheckLang { get; set; } = "";

    public MainViewModel(
        IFileService fileService,
        INormalizationService normalizationService,
        IMemoryTrackerService memoryTrackerService,
        ISettingsService settingsService)
    {
        _fileService = fileService;
        _normalizationService = normalizationService;
        _memoryTrackerService = memoryTrackerService;
        _settingsService = settingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _fileService.FileSaved += OnFileSaved;

        _idleTimer = new Timer();
        _idleTimer.AutoReset = false;
        _idleTimer.Elapsed += OnIdleElapsed;
        lock (_saveLock) { _lastUserActivity = DateTime.UtcNow; }
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        EditorContent = await _fileService.LoadAsync(ct);
        lock (_saveLock) { _lastSavedContent = EditorContent; }
        _fileService.Watch(OnExternalChange);
        await ConfigureIdleTimerAsync();
        StartIdleTimer();
    }

    private async Task ConfigureIdleTimerAsync()
    {
        var settings = await _settingsService.LoadAsync();
        _idleTimer.Interval = TimeSpan.FromMinutes(settings.IdleMinutesBeforeNormalize).TotalMilliseconds;
    }

    partial void OnEditorContentChanged(string value)
    {
        if (_isReloading) return;
        lock (_saveLock) { _lastUserActivity = DateTime.UtcNow; }
        SyncState = "Saving...";
        _fileService.QueueAutoSave(value);
    }

    private void OnFileSaved()
    {
        lock (_saveLock) { _lastSavedContent = EditorContent; }
        _dispatcherQueue.TryEnqueue(() => SyncState = "Sync ✓");
    }

    private void OnExternalChange(string newContent)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            string lastSaved;
            lock (_saveLock) { lastSaved = _lastSavedContent; }
            if (EditorContent == lastSaved)
            {
                EditorContent = newContent;
                lock (_saveLock) { _lastSavedContent = newContent; }
            }
            else
            {
                HasExternalChange = true;
            }
        });
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".txt");

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var settings = await _settingsService.LoadAsync();
        settings.NotesFilePath = file.Path;
        await _settingsService.SaveAsync(settings);

        _fileService.StopWatching();
        _fileService.CancelAutoSave();
        _isReloading = true;
        EditorContent = await _fileService.LoadAsync();
        lock (_saveLock) { _lastSavedContent = EditorContent; }
        _isReloading = false;
        _fileService.Watch(OnExternalChange);
        HasExternalChange = false;
        SyncState = "Sync ✓";
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
        _fileService.CancelAutoSave();
        _isReloading = true;
        EditorContent = await _fileService.LoadAsync();
        lock (_saveLock) { _lastSavedContent = EditorContent; }
        HasExternalChange = false;
        _isReloading = false;
        SyncState = "Rechargé";
    }

    [RelayCommand]
    private async Task NormalizeAsync() => await NormalizeInternalAsync(autoApply: false);

    private async Task NormalizeInternalAsync(bool autoApply)
    {
        if (string.IsNullOrWhiteSpace(EditorContent))
        {
            NormalizeStatus = "Rien à normaliser";
            return;
        }

        var activeSettings = await _settingsService.LoadAsync();

        if (_normalizationService.IsRateLimited(EditorContent, out var remaining, activeSettings.NormalizeRateLimitSeconds))
        {
            NormalizeStatus = $"Rate limit: attendre {remaining.Minutes:0}m {remaining.Seconds:0}s";
            return;
        }

        if (_normalizationService.ExceedsSizeLimit(EditorContent, out var reason))
        {
            NormalizeStatus = reason;
            return;
        }

        IsNormalizing = true;
        NormalizeStatus = "Normalisation en cours...";

        try
        {
            var result = await _normalizationService.NormalizeAsync(EditorContent, CancellationToken.None);

            if (result.HasChanges)
            {
                PendingNormalization = result;
                if (autoApply)
                {
                    await ApplyNormalizationAsync();
                }
                else
                {
                    NormalizeStatus = $"Aperçu: +{result.LinesAdded} -{result.LinesDeleted} ~{result.LinesModified}";
                    LastNormalizeTime = DateTime.UtcNow.ToString("HH:mm");
                }
            }
            else
            {
                NormalizeStatus = "Aucune modification nécessaire";
                LastNormalizeTime = DateTime.UtcNow.ToString("HH:mm");
            }
        }
        catch (Exception ex)
        {
            NormalizeStatus = $"Erreur: {ex.Message}";
        }
        finally
        {
            IsNormalizing = false;
        }
    }

    [RelayCommand]
    private async Task ApplyNormalizationAsync()
    {
        if (PendingNormalization is null)
        {
            return;
        }

        EditorContent = PendingNormalization.NormalizedContent;
        await _fileService.SaveAsync(EditorContent);

        // Track in memory
        var record = new ChangeRecord
        {
            Type = "Normalize",
            LinesChanged = PendingNormalization.LinesAdded + PendingNormalization.LinesDeleted + PendingNormalization.LinesModified,
            Preview = PendingNormalization.NormalizedContent[..Math.Min(60, PendingNormalization.NormalizedContent.Length)],
            BackupPath = PendingNormalization.BackupPath,
        };
        await _memoryTrackerService.AppendAsync(record);

        NormalizeStatus = $"Normalisé ✓ ({PendingNormalization.Duration.TotalSeconds:F1}s)";
        PendingNormalization = null;
    }

    [RelayCommand]
    private void CancelNormalization()
    {
        PendingNormalization = null;
        NormalizeStatus = "Annulé";
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

    public async Task OnWindowClosingAsync(CancellationToken ct = default)
    {
        _idleTimer.Stop();
        _fileService.CancelAutoSave();

        var settings = await _settingsService.LoadAsync(ct);
        if (settings.AutoNormalizeOnClose && !IsNormalizing && !string.IsNullOrWhiteSpace(EditorContent))
        {
            await NormalizeInternalAsync(autoApply: true);
        }

        _fileService.Dispose();
    }

    private void StartIdleTimer()
    {
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private async void OnIdleElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            DateTime lastActivity;
            lock (_saveLock) { lastActivity = _lastUserActivity; }
            var idleDuration = DateTime.UtcNow - lastActivity;
            if (idleDuration >= TimeSpan.FromMinutes(settings.IdleMinutesBeforeNormalize))
            {
                var tcs = new TaskCompletionSource();
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        if (!IsNormalizing && !string.IsNullOrWhiteSpace(EditorContent))
                        {
                            await NormalizeInternalAsync(autoApply: true);
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Idle] Normalize failed: {ex.Message}"); }
                    finally { tcs.TrySetResult(); }
                });
                await tcs.Task;
            }
        }
        catch (IOException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Idle] {ex.Message}");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => _idleTimer.Start());
        }
    }
}
