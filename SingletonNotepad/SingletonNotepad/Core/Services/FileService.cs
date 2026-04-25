namespace SingletonNotepad.Core.Services;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Implementation of IFileService for managing the singleton markdown file.
/// </summary>
public class FileService : IFileService
{
    private const string DefaultTemplate = "# My Singleton Notepad\n\nStart typing your notes here...\n";
    private FileSystemWatcher? _watcher;

    public string FilePath { get; set; } = string.Empty;

    public async Task<string> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException("FilePath must be set before loading.");
        }

        if (!File.Exists(FilePath))
        {
            // Auto-create with default template
            await File.WriteAllTextAsync(FilePath, DefaultTemplate, cancellationToken);
            return DefaultTemplate;
        }

        return await File.ReadAllTextAsync(FilePath, cancellationToken);
    }

    public async Task SaveAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException("FilePath must be set before saving.");
        }

        // Retry logic with exponential backoff
        int attempt = 0;
        const int maxAttempts = 3;
        
        while (true)
        {
            try
            {
                await File.WriteAllTextAsync(FilePath, content, cancellationToken);
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts - 1)
            {
                attempt++;
                int delay = 200 * (int)Math.Pow(2, attempt); // 200ms, 400ms, 800ms
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public IDisposable WatchAsync(Action<string> onChanged)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException("FilePath must be set before watching.");
        }

        var directory = Path.GetDirectoryName(FilePath) ?? throw new DirectoryNotFoundException();
        var fileName = Path.GetFileName(FilePath);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += (sender, e) =>
        {
            // FileSystemWatcher raises on background thread
            onChanged?.Invoke(e.FullPath);
        };

        _watcher.EnableRaisingEvents = true;
        return new WatcherDisposable(_watcher);
    }

    private class WatcherDisposable : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private bool _disposed;

        public WatcherDisposable(FileSystemWatcher watcher)
        {
            _watcher = watcher;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _disposed = true;
            }
        }
    }
}
