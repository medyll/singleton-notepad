namespace SingletonNotepad.Core.Services;

/// <summary>
/// Service for tracking changes in the singleton file.
/// </summary>
public interface IMemoryTrackerService
{
    /// <summary>
    /// Appends a change record to the memory tracking file.
    /// </summary>
    Task AppendAsync(ChangeRecord record, CancellationToken cancellationToken = default);
}
