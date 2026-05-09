namespace SingletonNotepad.Core.Providers;

public interface ILlmProviderSelector
{
    ILlmProvider Current { get; }
    string CurrentName { get; }
    void SelectProvider(string providerName);
    ILlmProvider? GetProvider(string providerName);
    IReadOnlyList<string> AvailableProviders { get; }
}