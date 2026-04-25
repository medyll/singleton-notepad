namespace SingletonNotepad.Core.Helpers;

using System.Runtime.InteropServices;

/// <summary>
/// Helper for monitoring and window positioning across monitors.
/// </summary>
public static class MonitorHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const uint MONITOR_DEFAULTTOPRIMARY = 0x00020000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOSIZE = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    /// <summary>
    /// Checks if the given coordinates are within the primary monitor bounds.
    /// </summary>
    public static bool IsOnPrimaryMonitor(double x, double y)
    {
        var screenLeft = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Left ?? 0;
        var screenTop = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Top ?? 0;
        var screenRight = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Right ?? 0;
        var screenBottom = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Bottom ?? 0;

        return x >= screenLeft && x < screenRight && y >= screenTop && y < screenBottom;
    }

    /// <summary>
    /// Centers the window on the primary monitor.
    /// </summary>
    public static void CenterOnPrimaryMonitor(IntPtr hwnd, int width, int height)
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        var x = screen.WorkingArea.Left + (screen.WorkingArea.Width - width) / 2;
        var y = screen.WorkingArea.Top + (screen.WorkingArea.Height - height) / 2;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER);
    }
}
