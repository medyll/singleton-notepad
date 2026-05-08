using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Core.Services;

public class ModelService : IModelService
{
    private readonly IReadOnlyDictionary<string, ILlmProvider> _providers;

    public ModelService(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
    }

    public async Task<IReadOnlyList<string>> GetModelsForProviderAsync(string providerName, CancellationToken ct = default)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
            return [];

        try
        {
            return await provider.GetAvailableModelsAsync(ct);
        }
        catch
        {
            return [];
        }
    }
}
