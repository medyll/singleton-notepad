using Microsoft.UI.Xaml;
using SingletonNotepad.Core.Helpers;
using SingletonNotepad.Core.Services;
using SingletonNotepad.Views;

namespace SingletonNotepad;

/// <summary>
/// Main application window with positioning and lifecycle management.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private const int DefaultWidth = 1200;
    private const int DefaultHeight = 800;

    public MainWindow(
        ISettingsService settingsService,
        MainView mainView)
    {
        InitializeComponent();
        _settingsService = settingsService;
        
        Content = mainView;
        
        this.Activated += OnActivated;
        this.Closed += OnClosed;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        // Restore window position on first activation
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            RestoreWindowPosition();
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        // Save window position
        var bounds = this.Bounds;
        _settingsService.Set("WindowLeft", bounds.X);
        _settingsService.Set("WindowTop", bounds.Y);
        _settingsService.Set("WindowWidth", bounds.Width);
        _settingsService.Set("WindowHeight", bounds.Height);
    }

    private void RestoreWindowPosition()
    {
        var left = _settingsService.Get("WindowLeft", double.NaN);
        var top = _settingsService.Get("WindowTop", double.NaN);
        var width = _settingsService.Get("WindowWidth", (double)DefaultWidth);
        var height = _settingsService.Get("WindowHeight", (double)DefaultHeight);

        // Validate position is on primary monitor
        if (double.IsNaN(left) || double.IsNaN(top) || 
            !MonitorHelper.IsOnPrimaryMonitor(left, top))
        {
            // Center on primary monitor
            MonitorHelper.CenterOnPrimaryMonitor(
                WinUI.Interop.GetWindowHandle(this), 
                (int)width, 
                (int)height);
        }
        else
        {
            // Restore saved position
            WinUI.Interop.SetWindowPos(
                WinUI.Interop.GetWindowHandle(this),
                IntPtr.Zero,
                (int)left,
                (int)top,
                (int)width,
                (int)height,
                0);
        }
    }
}
