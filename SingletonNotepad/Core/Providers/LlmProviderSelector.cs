namespace SingletonNotepad.Core.Providers;

public class LlmProviderSelector : ILlmProviderSelector
{
    private readonly Dictionary<string, ILlmProvider> _providers;
    private readonly IReadOnlyList<string> _availableProviders;
    private volatile ILlmProvider _current;

    public LlmProviderSelector(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _current = _providers.Values.FirstOrDefault()
            ?? throw new InvalidOperationException("No LLM providers registered.");
        _availableProviders = _providers.Keys.ToList().AsReadOnly();
    }

    public ILlmProvider Current => _current;

    public string CurrentName => _current.Name;

    public IReadOnlyList<string> AvailableProviders => _availableProviders;

    public ILlmProvider? GetProvider(string providerName)
        => _providers.TryGetValue(providerName, out var p) ? p : null;

    public void SelectProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var provider))
        {
            _current = provider;
        }
    }
}