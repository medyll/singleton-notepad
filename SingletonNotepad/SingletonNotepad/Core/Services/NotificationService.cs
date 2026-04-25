namespace SingletonNotepad.Core.Services;

using Avalonia.Controls.Notifications;

/// <summary>
/// Implementation of INotificationService using Avalonia notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    public void SetManager(WindowNotificationManager manager)
    {
        _manager = manager;
    }

    public void Show(string message)
    {
        if (_manager == null)
        {
            System.Diagnostics.Debug.WriteLine($"[Notification] {message}");
            return;
        }

        _manager.Show(new Notification("Singleton Notepad", message, NotificationType.Information));
    }

    public void ShowError(string message)
    {
        if (_manager == null)
        {
            System.Diagnostics.Debug.WriteLine($"[Notification ERROR] {message}");
            return;
        }

        _manager.Show(new Notification("Error", message, NotificationType.Error));
    }
}
