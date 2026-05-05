namespace SingletonNotepad.Core.Providers;

public class LlmProviderSelector : ILlmProviderSelector
{
    private readonly Dictionary<string, ILlmProvider> _providers;
    private ILlmProvider _current;

    public LlmProviderSelector(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, p => p);
        _current = _providers.Values.First();
    }

    public ILlmProvider Current => _current;

    public string CurrentName => _current.Name;

    public IReadOnlyList<string> AvailableProviders => _providers.Keys.ToList();

    public void SelectProvider(string providerName)
    {
        if (_providers.TryGetValue(providerName, out var provider))
        {
            _current = provider;
        }
    }
}