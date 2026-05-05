using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public interface IMemoryTrackerService
{
    Task AppendAsync(ChangeRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ChangeRecord>> GetHistoryAsync(int maxRecords = 50, CancellationToken ct = default);
}
