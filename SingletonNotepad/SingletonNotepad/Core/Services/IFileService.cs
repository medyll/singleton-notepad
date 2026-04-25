namespace SingletonNotepad.Core.Services;

/// <summary>
/// Service for file operations on the singleton markdown file.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Gets or sets the path to the singleton file.
    /// </summary>
    string FilePath { get; set; }

    /// <summary>
    /// Loads the content of the singleton file asynchronously.
    /// Creates the file with empty template if it doesn't exist.
    /// </summary>
    Task<string> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content to the singleton file asynchronously.
    /// </summary>
    Task SaveAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts watching the file for external changes.
    /// Returns a disposable that stops watching when disposed.
    /// </summary>
    IDisposable WatchAsync(Action<string> onChanged);
}
