namespace SingletonNotepad.Core.Services;

using SingletonNotepad.Core.Models;

/// <summary>
/// Service for tracking changes in a local memory file.
/// </summary>
public interface IMemoryTrackerService
{
    /// <summary>
    /// Appends a change record to the memory tracking file.
    /// </summary>
    Task AppendAsync(ChangeRecord record, CancellationToken cancellationToken = default);
}
