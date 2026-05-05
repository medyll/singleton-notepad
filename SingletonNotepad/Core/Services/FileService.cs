using System.Timers;
using Timer = System.Timers.Timer;

namespace SingletonNotepad.Core.Services;

/// <summary>
/// Stub implementation — full implementation in S1-02.
/// </summary>
public class FileService : IFileService
{
    private readonly Timer _autoSaveTimer;
    private string _pendingContent = string.Empty;

    public event Action? FileSaved;

    public FileService()
    {
        _autoSaveTimer = new Timer(2000);
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Elapsed += OnAutoSaveElapsed;
    }

    public Task<string> LoadAsync(CancellationToken ct = default)
    {
        // Stub: return empty content. Full impl in S1-02.
        return Task.FromResult(string.Empty);
    }

    public Task SaveAsync(string content, CancellationToken ct = default)
    {
        // Stub: no-op. Full impl in S1-02.
        FileSaved?.Invoke();
        return Task.CompletedTask;
    }

    public void Watch(Action<string> onExternalChange)
    {
        // Stub: no-op. Full impl in S1-02.
    }

    public void StopWatching()
    {
        // Stub: no-op.
    }

    public void QueueAutoSave(string content)
    {
        _pendingContent = content;
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    public void CancelAutoSave()
    {
        _autoSaveTimer.Stop();
        _pendingContent = string.Empty;
    }

    private void OnAutoSaveElapsed(object? sender, ElapsedEventArgs e)
    {
        // Stub: will be wired to actual save in S1-02.
        FileSaved?.Invoke();
    }
}
