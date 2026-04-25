using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT;

namespace SingletonNotepad.WinUI;

/// <summary>
/// Interop helpers for WinUI 3 window operations.
/// </summary>
public static class Interop
{
    public const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Gets the Win32 window handle for a WinUI 3 window.
    /// </summary>
    public static IntPtr GetWindowHandle(Window window)
    {
        var windowNative = window.As<IWindowNative>();
        return windowNative.WindowId;
    }

    /// <summary>
    /// Sets the position and size of a window.
    /// </summary>
    public static void SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags)
    {
        SetWindowPos(hwnd, insertAfter, x, y, width, height, flags);
    }

    /// <summary>
    /// Restores a minimized window or shows it normally.
    /// </summary>
    public static void ShowWindow(IntPtr hwnd, int nCmdShow) => ShowWindow(hwnd, nCmdShow);

    /// <summary>
    /// Brings a window to the foreground.
    /// </summary>
    public static bool SetForegroundWindow(IntPtr hwnd) => SetForegroundWindow(hwnd);

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("EECDBCF0-E169-11D0-8549-00AA00380B78")]
    private interface IWindowNative
    {
        IntPtr WindowId { get; }
    }
}
