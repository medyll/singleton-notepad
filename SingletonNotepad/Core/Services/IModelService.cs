namespace SingletonNotepad.Core.Services;

public interface IModelService
{
    Task<IReadOnlyList<string>> GetModelsForProviderAsync(string providerName, CancellationToken ct = default);
}
