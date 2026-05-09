namespace SingletonNotepad.Core.Services;

/// <summary>
/// Responsible for reading, writing, and watching a single Markdown file.
/// </summary>
public interface IFileService : IDisposable
{
    /// <summary>
    /// Loads the content of the singleton file. Auto-creates if absent.
    /// </summary>
    Task<string> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves content to the singleton file with retry logic.
    /// </summary>
    Task SaveAsync(string content, CancellationToken ct = default);

    /// <summary>
    /// Starts watching the file for external modifications.
    /// </summary>
    void Watch(Action<string> onExternalChange);

    /// <summary>
    /// Stops the file watcher.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Queues an auto-save (resets 2s debounce timer).
    /// </summary>
    void QueueAutoSave(string content);

    /// <summary>
    /// Cancels any pending auto-save.
    /// </summary>
    void CancelAutoSave();

    /// <summary>
    /// Event raised after a successful save.
    /// </summary>
    event Action? FileSaved;

    /// <summary>
    /// Event raised when an external change is detected by the watcher.
    /// </summary>
    event Action<string>? ExternalChangeDetected;
}
