namespace SingletonNotepad.Core.Services;

/// <summary>
/// Service for displaying toast notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a toast notification with the specified message.
    /// </summary>
    void Show(string message);

    /// <summary>
    /// Shows an error toast notification.
    /// </summary>
    void ShowError(string message);
}
