namespace SingletonNotepad.Core.Providers;

public interface ILlmProvider
{
    string Name { get; }
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default);
}
