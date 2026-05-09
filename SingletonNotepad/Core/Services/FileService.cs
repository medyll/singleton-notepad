using System.Security.Cryptography;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SingletonNotepad.Core.Services;

/// <summary>
/// Reads, writes, and watches a single Markdown file.
/// Auto-creates the file if absent. Auto-saves with 2s debounce.
/// </summary>
public class FileService : IFileService, IDisposable
{
    private const string DefaultFileName = "MY_SINGLETON_NOTEPAD.md";
    private const int ExternalChangeLockReleaseMs = 150;

    private readonly ISettingsService _settingsService;
    private readonly Timer _autoSaveTimer;
    private readonly Timer _externalChangeDebounce;
    private FileSystemWatcher? _watcher;
    private Action<string>? _externalChangeSubscriber;
    private string _pendingContent = string.Empty;
    private string? _currentFilePath;
    private string _lastWrittenHash = string.Empty;
    private int _isSaving;

    public event Action? FileSaved;
    public event Action<string>? ExternalChangeDetected;

    public FileService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        _autoSaveTimer = new Timer(2000) { AutoReset = false };
        _autoSaveTimer.Elapsed += OnAutoSaveElapsed;

        _externalChangeDebounce = new Timer(300) { AutoReset = false };
        _externalChangeDebounce.Elapsed += OnExternalChangeDebounceElapsed;
    }

    public async Task<string> LoadAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var path = settings.NotesFilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            var docsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(docsFolder, DefaultFileName);
            settings.NotesFilePath = path;
            await _settingsService.SaveAsync(settings, ct);
        }

        _currentFilePath = path;

        if (!File.Exists(path))
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, string.Empty, ct);
            _lastWrittenHash = Hash(string.Empty);
            return string.Empty;
        }

        var content = await ReadWithRetryAsync(path, ct);
        _lastWrittenHash = Hash(content);
        return content;
    }

    public async Task SaveAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            await LoadAsync(ct);
        }

        await WriteWithRetryAsync(_currentFilePath!, content, ct);
        _lastWrittenHash = Hash(content);
        FileSaved?.Invoke();
    }

    public void Watch(Action<string> onExternalChange)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            return;
        }

        StopWatching();

        _externalChangeSubscriber = onExternalChange;
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
            _watcher.Changed -= OnFileChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_externalChangeSubscriber != null)
        {
            ExternalChangeDetected -= _externalChangeSubscriber;
            _externalChangeSubscriber = null;
        }

        _externalChangeDebounce.Stop();
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
        if (Interlocked.CompareExchange(ref _isSaving, 1, 0) != 0) return;

        try
        {
            await SaveAsync(_pendingContent);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FileService] Auto-save IO error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"[FileService] Auto-save error: {ex.Message}");
        }
        finally
        {
            _isSaving = 0;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Coalesce burst of Changed events Windows fires per save
        _externalChangeDebounce.Stop();
        _externalChangeDebounce.Start();
    }

    private async void OnExternalChangeDebounceElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
                return;

            await Task.Delay(ExternalChangeLockReleaseMs);

            var content = await ReadWithRetryAsync(_currentFilePath, CancellationToken.None);
            var contentHash = Hash(content);

            // If content matches what we just wrote, suppress (it's our own save echo)
            if (contentHash == _lastWrittenHash) return;

            _lastWrittenHash = contentHash;
            ExternalChangeDetected?.Invoke(content);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FileService] External change read error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Debug.WriteLine($"[FileService] External change error: {ex.Message}");
        }
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

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public void Dispose()
    {
        StopWatching();
        _autoSaveTimer.Stop();
        _autoSaveTimer.Dispose();
        _externalChangeDebounce.Dispose();
        GC.SuppressFinalize(this);
    }
}
