using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public interface INormalizationService
{
    Task<NormalizationResult> NormalizeAsync(string content, CancellationToken ct = default);
    Task<NormalizationRule> LoadRulesAsync(CancellationToken ct = default);
    bool IsRateLimited(string content, out TimeSpan remaining);
    bool ExceedsSizeLimit(string content, out string reason);
    Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default);
    string RulesFilePath { get; }
}

public class BackupInfo
{
    public required string Path { get; init; }
    public required DateTime Timestamp { get; init; }
    public required int Version { get; init; }
    public required long SizeBytes { get; init; }
}
