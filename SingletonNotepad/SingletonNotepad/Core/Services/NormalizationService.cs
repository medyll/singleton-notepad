namespace SingletonNotepad.Core.Services;

using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Stub implementation of INormalizationService for Sprint 1.
/// Full implementation with LLM providers in Sprint 2.
/// </summary>
public class NormalizationService : INormalizationService
{
    public async Task<NormalizationResult> NormalizeAsync(
        string content,
        string rulesPath,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement full normalization in Sprint 2
        // For now, return content unchanged
        await Task.Delay(100, cancellationToken); // Simulate minimal work
        
        return new NormalizationResult
        {
            OriginalContent = content,
            NormalizedContent = content,
            HasChanges = false,
            DiffHtml = null,
            Elapsed = TimeSpan.FromMilliseconds(100)
        };
    }
}
