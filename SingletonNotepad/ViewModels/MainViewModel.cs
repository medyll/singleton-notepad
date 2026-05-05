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
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Timer _idleTimer;
    private const int DefaultIdleMinutes = 15;
    private DateTime _lastUserActivity;

    [ObservableProperty]
    public partial string EditorContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SyncState { get; set; } = "Ready";

    [ObservableProperty]
    public partial string NormalizeStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNormalizing { get; set; }

    [ObservableProperty]
    public partial bool IsShowingDiff { get; set; }

    [ObservableProperty]
    public partial NormalizationResult? PendingNormalization { get; set; }

    public MainViewModel(IFileService fileService, INormalizationService normalizationService, IMemoryTrackerService memoryTrackerService)
    {
        _fileService = fileService;
        _normalizationService = normalizationService;
        _memoryTrackerService = memoryTrackerService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _fileService.FileSaved += OnFileSaved;

        _idleTimer = new Timer(TimeSpan.FromMinutes(DefaultIdleMinutes).TotalMilliseconds);
        _idleTimer.AutoReset = false;
        _idleTimer.Elapsed += OnIdleElapsed;
        _lastUserActivity = DateTime.Now;
    }

    public async Task LoadContentAsync(CancellationToken ct = default)
    {
        EditorContent = await _fileService.LoadAsync(ct);
        _fileService.Watch(OnExternalChange);
        StartIdleTimer();
    }

    partial void OnEditorContentChanged(string value)
    {
        _lastUserActivity = DateTime.Now;
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
        if (string.IsNullOrWhiteSpace(EditorContent))
        {
            NormalizeStatus = "Rien à normaliser";
            return;
        }

        if (_normalizationService.IsRateLimited(EditorContent, out var remaining))
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
            var result = await _normalizationService.NormalizeAsync(EditorContent);

            if (result.HasChanges)
            {
                PendingNormalization = result;
                IsShowingDiff = true;
                NormalizeStatus = $"Aperçu: +{result.LinesAdded} -{result.LinesDeleted} ~{result.LinesModified}";
            }
            else
            {
                NormalizeStatus = "Aucune modification nécessaire";
                PendingNormalization = result;
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
        IsShowingDiff = false;
        PendingNormalization = null;
    }

    [RelayCommand]
    private void CancelNormalization()
    {
        IsShowingDiff = false;
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

        if (_fileService is IDisposable fs)
        {
            fs.Dispose();
        }
    }

    private void StartIdleTimer()
    {
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private void OnIdleElapsed(object? sender, ElapsedEventArgs e)
    {
        var idleDuration = DateTime.Now - _lastUserActivity;
        if (idleDuration >= TimeSpan.FromMinutes(DefaultIdleMinutes) && !IsNormalizing && !string.IsNullOrWhiteSpace(EditorContent))
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await NormalizeAsync();
            });
        }

        _dispatcherQueue.TryEnqueue(() => _idleTimer.Start());
    }
}
