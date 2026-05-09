using System.Diagnostics;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace SingletonNotepad.Core.Helpers;

/// <summary>
/// Helper for primary monitor window positioning using DisplayArea API.
/// </summary>
public static class MonitorHelper
{
    /// <summary>
    /// Checks if >50% of the window overlaps the primary monitor work area.
    /// </summary>
    public static bool IsWindowValidOnPrimaryMonitor(int x, int y, int width, int height)
    {
        try
        {
            var workArea = DisplayArea.Primary.WorkArea;
            var windowRect = new RectInt32(x, y, width, height);

            var overlapX = Math.Max(0, Math.Min(windowRect.X + windowRect.Width, workArea.X + workArea.Width) - Math.Max(windowRect.X, workArea.X));
            var overlapY = Math.Max(0, Math.Min(windowRect.Y + windowRect.Height, workArea.Y + workArea.Height) - Math.Max(windowRect.Y, workArea.Y));

            var overlapArea = overlapX * overlapY;
            var windowArea = width * height;

            return windowArea > 0 && overlapArea > windowArea / 2;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MonitorHelper] IsWindowValidOnPrimaryMonitor error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns (x, y) centered in the primary monitor work area.
    /// </summary>
    public static (int x, int y) CenterOnPrimaryMonitor(int width, int height)
    {
        try
        {
            var workArea = DisplayArea.Primary.WorkArea;
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (workArea.Height - height) / 2;
            return (x, y);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MonitorHelper] CenterOnPrimaryMonitor error: {ex.Message}");
            return (0, 0);
        }
    }

    /// <summary>
    /// Returns (x, y) centered horizontally and flush to the bottom of the primary work area.
    /// </summary>
    public static (int x, int y) BottomCenterOnPrimaryMonitor(int width, int height)
    {
        try
        {
            var workArea = DisplayArea.Primary.WorkArea;
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + workArea.Height - height - 12;
            return (x, y);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MonitorHelper] BottomCenterOnPrimaryMonitor error: {ex.Message}");
            return (0, 0);
        }
    }

    /// <summary>
    /// Ensures a window position is valid on the primary monitor, returning a safe position.
    /// </summary>
    public static (int x, int y) EnsureValidWindowPosition(int x, int y, int width, int height)
    {
        if (IsWindowValidOnPrimaryMonitor(x, y, width, height))
        {
            return (x, y);
        }
        return CenterOnPrimaryMonitor(width, height);
    }
}
