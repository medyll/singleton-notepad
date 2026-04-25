namespace SingletonNotepad.Core.Services;

/// <summary>
/// Stub implementation of INotificationService for Sprint 1.
/// Full implementation with WinUI 3 toasts in Sprint 3.
/// </summary>
public class NotificationService : INotificationService
{
    public void Show(string message)
    {
        // TODO: Implement WinUI 3 toast in Sprint 3
        System.Diagnostics.Debug.WriteLine($"[Notification] {message}");
    }

    public void ShowError(string message)
    {
        // TODO: Implement WinUI 3 error toast in Sprint 3
        System.Diagnostics.Debug.WriteLine($"[Notification ERROR] {message}");
    }
}
