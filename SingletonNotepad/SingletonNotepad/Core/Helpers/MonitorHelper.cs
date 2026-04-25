namespace SingletonNotepad.Core.Helpers;

using System.Runtime.InteropServices;

/// <summary>
/// Helper for monitoring and window positioning across monitors.
/// </summary>
public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint SWP_NOZORDER = 0x0004;

    /// <summary>
    /// Checks if a window with the given bounds is sufficiently visible on the primary monitor.
    /// At least 50% of the window must be visible to pass validation.
    /// </summary>
    public static bool IsWindowValidOnPrimaryMonitor(double left, double top, double width, double height)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return false;

        var workArea = screen.WorkingArea;
        
        // Calculate window rectangle
        var windowRight = left + width;
        var windowBottom = top + height;
        
        // Check if window is completely off-screen
        if (windowRight < workArea.Left || left > workArea.Right ||
            windowBottom < workArea.Top || top > workArea.Bottom)
        {
            return false;
        }

        // Calculate visible area
        var visibleLeft = Math.Max(left, workArea.Left);
        var visibleTop = Math.Max(top, workArea.Top);
        var visibleRight = Math.Min(windowRight, workArea.Right);
        var visibleBottom = Math.Min(windowBottom, workArea.Bottom);

        var visibleWidth = Math.Max(0, visibleRight - visibleLeft);
        var visibleHeight = Math.Max(0, visibleBottom - visibleTop);
        var visibleArea = visibleWidth * visibleHeight;
        var windowArea = width * height;

        // At least 50% of the window must be visible
        return windowArea > 0 && visibleArea / windowArea >= 0.5;
    }

    /// <summary>
    /// Checks if the given point is within the primary monitor bounds.
    /// </summary>
    public static bool IsOnPrimaryMonitor(double x, double y)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return false;

        return x >= screen.Bounds.Left && x < screen.Bounds.Right &&
               y >= screen.Bounds.Top && y < screen.Bounds.Bottom;
    }

    /// <summary>
    /// Gets the primary monitor's working area (excluding taskbar).
    /// </summary>
    public static (int Left, int Top, int Width, int Height) GetPrimaryMonitorWorkArea()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return (0, 0, 1920, 1080); // Fallback to common resolution

        return (screen.WorkingArea.Left, screen.WorkingArea.Top, 
                screen.WorkingArea.Width, screen.WorkingArea.Height);
    }

    /// <summary>
    /// Centers the window on the primary monitor.
    /// </summary>
    public static void CenterOnPrimaryMonitor(IntPtr hwnd, int width, int height)
    {
        var (left, top, _, _) = GetPrimaryMonitorWorkArea();
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        var x = left + (screen.WorkingArea.Width - width) / 2;
        var y = top + (screen.WorkingArea.Height - height) / 2;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER);
    }

    /// <summary>
    /// Ensures window bounds are valid for the current monitor configuration.
    /// If invalid, returns centered position on primary monitor.
    /// </summary>
    public static (int X, int Y) EnsureValidWindowPosition(double left, double top, double width, double height)
    {
        if (IsWindowValidOnPrimaryMonitor(left, top, width, height))
        {
            return ((int)left, (int)top);
        }

        // Fall back to centered position
        var (_, _, screenWidth, screenHeight) = GetPrimaryMonitorWorkArea();
        var x = (screenWidth - (int)width) / 2;
        var y = (screenHeight - (int)height) / 2;
        
        return (x, y);
    }
}
