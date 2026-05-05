using System.Timers;
using Timer = System.Timers.Timer;

namespace SingletonNotepad.Core.Services;

/// <summary>
/// Reads, writes, and watches a single Markdown file.
/// Auto-creates the file if absent. Auto-saves with 2s debounce.
/// </summary>
public class FileService : IFileService
{
    private readonly ISettingsService _settingsService;
    private readonly Timer _autoSaveTimer;
    private FileSystemWatcher? _watcher;
    private string _pendingContent = string.Empty;
    private string? _currentFilePath;
    private bool _isSaving;

    public event Action? FileSaved;
    public event Action<string>? ExternalChangeDetected;

    public FileService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        _autoSaveTimer = new Timer(2000);
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Elapsed += OnAutoSaveElapsed;
    }

    public async Task<string> LoadAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var path = settings.NotesFilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            // Default path: Documents\MY_SINGLETON_NOTEPAD.md
            var docsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(docsFolder, "MY_SINGLETON_NOTEPAD.md");
            settings.NotesFilePath = path;
            await _settingsService.SaveAsync(settings, ct);
        }

        _currentFilePath = path;

        // Auto-create if absent
        if (!File.Exists(path))
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, string.Empty, ct);
            return string.Empty;
        }

        var content = await ReadWithRetryAsync(path, ct);
        return content;
    }

    public async Task SaveAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            await LoadAsync(ct);
        }

        await WriteWithRetryAsync(_currentFilePath!, content, ct);
        FileSaved?.Invoke();
    }

    public void Watch(Action<string> onExternalChange)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            return;
        }

        ExternalChangeDetected += onExternalChange;

        _watcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(_currentFilePath)!,
            Filter = Path.GetFileName(_currentFilePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileChanged;
    }

    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void QueueAutoSave(string content)
    {
        _pendingContent = content;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    public void CancelAutoSave()
    {
        _autoSaveTimer.Stop();
        _pendingContent = string.Empty;
    }

    private async void OnAutoSaveElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isSaving || string.IsNullOrEmpty(_pendingContent))
        {
            return;
        }

        _isSaving = true;
        try
        {
            await SaveAsync(_pendingContent);
        }
        catch
        {
            // Best-effort auto-save; user can manually save if needed
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Marshal to avoid blocking the watcher thread
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500); // Debounce rapid changes
                if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
                {
                    var content = await File.ReadAllTextAsync(_currentFilePath);
                    ExternalChangeDetected?.Invoke(content);
                }
            }
            catch
            {
                // File might be locked by another process
            }
        });
    }

    private static async Task<string> ReadWithRetryAsync(string path, CancellationToken ct)
    {
        const int maxRetries = 3;
        var delays = new[] { 200, 400, 800 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await File.ReadAllTextAsync(path, ct);
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(delays[attempt], ct);
            }
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    private static async Task WriteWithRetryAsync(string path, string content, CancellationToken ct)
    {
        const int maxRetries = 3;
        var delays = new[] { 200, 400, 800 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, content, ct);
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(delays[attempt], ct);
            }
        }

        await File.WriteAllTextAsync(path, content, ct);
    }
}
