namespace SingletonNotepad.Core.Helpers;

/// <summary>
/// Helper for window positioning (Avalonia-compatible stub).
/// </summary>
public static class MonitorHelper
{
    /// <summary>
    /// Ensures window bounds are valid for the current monitor configuration.
    /// </summary>
    public static (int X, int Y) EnsureValidWindowPosition(double left, double top, double width, double height)
    {
        // Simple validation - keep within reasonable bounds
        if (left < -1000 || top < -1000 || left > 10000 || top > 10000)
        {
            return (0, 0);
        }

        return ((int)left, (int)top);
    }

    /// <summary>
    /// Centers the window on the primary monitor (stub for Avalonia).
    /// </summary>
    public static void CenterOnPrimaryMonitor(IntPtr hwnd, int width, int height)
    {
        // Not implemented for Avalonia - window positioning handled by Avalonia
    }
}
