namespace SingletonNotepad.Core.Services;

/// <summary>
/// Service for file operations on the singleton markdown file.
/// </summary>
public interface IFileService : IDisposable
{
    string FilePath { get; set; }

    /// <summary>Raised on the thread-pool after a debounced save completes successfully.</summary>
    event Action? FileSaved;

    /// <summary>Raised on the thread-pool when a debounced save fails.</summary>
    event Action<Exception>? SaveFailed;

    /// <summary>
    /// Loads the singleton file, auto-creating it with the default template if absent.
    /// </summary>
    Task<string> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content immediately with exponential-backoff retry (3 attempts).
    /// </summary>
    Task SaveAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a debounced save (2 s). Resets the timer on each call.
    /// Raises <see cref="FileSaved"/> or <see cref="SaveFailed"/> on completion.
    /// </summary>
    void ScheduleSave(string content);

    /// <summary>
    /// Starts watching the file for external changes.
    /// Returns a disposable that stops watching when disposed.
    /// </summary>
    IDisposable Watch(Action<string> onChanged);
}
