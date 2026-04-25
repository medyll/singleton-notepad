namespace SingletonNotepad.Core.Services;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SingletonNotepad.Core.Models;

/// <summary>
/// Stub implementation of IMemoryTrackerService for Sprint 1.
/// Full implementation in Sprint 2.
/// </summary>
public class MemoryTrackerService : IMemoryTrackerService
{
    public Task AppendAsync(ChangeRecord record, CancellationToken cancellationToken = default)
    {
        // TODO: Implement in Sprint 2
        // For now, just log to debug output
        System.Diagnostics.Debug.WriteLine($"[MemoryTracker] Would record: {record.Type} at {record.Timestamp}");
        return Task.CompletedTask;
    }
}
