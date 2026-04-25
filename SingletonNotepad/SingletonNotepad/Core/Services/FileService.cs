namespace SingletonNotepad.Core.Services;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class FileService : IFileService
{
    private const string DefaultTemplate = "# My Singleton Notepad\n\nStart typing your notes here...\n";
    private const int DebounceMs = 2000;

    private Timer? _debounceTimer;
    private volatile string? _pendingContent;
    private bool _disposed;

    public string FilePath { get; set; } = string.Empty;

    public event Action? FileSaved;
    public event Action<Exception>? SaveFailed;

    public async Task<string> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("FilePath must be set before loading.");

        if (!File.Exists(FilePath))
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(FilePath, DefaultTemplate, cancellationToken);
            return DefaultTemplate;
        }

        return await File.ReadAllTextAsync(FilePath, cancellationToken);
    }

    public async Task SaveAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("FilePath must be set before saving.");

        int attempt = 0;
        while (true)
        {
            try
            {
                await File.WriteAllTextAsync(FilePath, content, cancellationToken);
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                attempt++;
                await Task.Delay(200 * (int)Math.Pow(2, attempt), cancellationToken);
            }
        }
    }

    public void ScheduleSave(string content)
    {
        _pendingContent = content;

        if (_debounceTimer == null)
            _debounceTimer = new Timer(OnDebounceElapsed, null, DebounceMs, Timeout.Infinite);
        else
            _debounceTimer.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnDebounceElapsed(object? state)
    {
        var content = _pendingContent;
        if (content == null) return;
        
        // Fire-and-forget with proper error handling
        // Using async void here is acceptable for timer callbacks
        // as long as errors are caught and propagated via events
        _ = SaveAndNotifyAsync(content);
    }

    private async Task SaveAndNotifyAsync(string content)
    {
        try
        {
            await SaveAsync(content);
            FileSaved?.Invoke();
        }
        catch (Exception ex)
        {
            SaveFailed?.Invoke(ex);
        }
    }

    public IDisposable Watch(Action<string> onChanged)
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("FilePath must be set before watching.");

        var directory = Path.GetDirectoryName(FilePath) ?? throw new DirectoryNotFoundException();
        var fileName = Path.GetFileName(FilePath);

        var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, e) => onChanged(e.FullPath);

        return new WatcherDisposable(watcher);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    private sealed class WatcherDisposable : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private bool _disposed;

        public WatcherDisposable(FileSystemWatcher watcher) => _watcher = watcher;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
