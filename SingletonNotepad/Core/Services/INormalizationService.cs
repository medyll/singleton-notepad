using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public interface INormalizationService
{
    Task<NormalizationResult> NormalizeAsync(string content, CancellationToken ct = default);
    Task<NormalizationRule> LoadRulesAsync(CancellationToken ct = default);
    bool IsRateLimited(string content, out TimeSpan remaining);
    bool ExceedsSizeLimit(string content, out string reason);
}
