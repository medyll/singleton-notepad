namespace SingletonNotepad.Core.Providers;

public interface ILlmProvider
{
    string Name { get; }
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
}
