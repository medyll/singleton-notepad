using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    private readonly MainView _mainView;
    private readonly SettingsView _settingsView;
    private const int DefaultWidth = 1200;
    private const int DefaultHeight = 800;

    public MainWindow(
        ISettingsService settingsService,
        MainView mainView,
        SettingsView settingsView)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _mainView = mainView;
        _settingsView = settingsView;
        
        // Start with MainView
        Content = _mainView;
        
        // Wire up settings navigation
        _mainView.RequestSettings += OnRequestSettings;
        _settingsView.ViewModel.RequestClose += OnRequestCloseSettings;
        
        this.Activated += OnActivated;
        this.Closed += OnClosed;
    }

    private void OnRequestSettings(object? sender, EventArgs e)
    {
        Content = _settingsView;
    }

    private void OnRequestCloseSettings(object? sender, EventArgs e)
    {
        Content = _mainView;
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
        // Don't save if window is minimized or maximized
        if (this.PresenterState != WindowPresentMode.Default) return;
        
        var bounds = this.Bounds;
        
        // Validate bounds are reasonable (minimum usable window size)
        if (bounds.Width < 400 || bounds.Height < 300) return;
        
        // Save via AppSettings model
        var settings = _settingsService.GetSettings();
        settings.WindowLeft = bounds.X;
        settings.WindowTop = bounds.Y;
        settings.WindowWidth = bounds.Width;
        settings.WindowHeight = bounds.Height;
        _settingsService.SaveSettings(settings);
    }

    private void RestoreWindowPosition()
    {
        var settings = _settingsService.GetSettings();
        
        var left = settings.WindowLeft;
        var top = settings.WindowTop;
        var width = settings.WindowWidth;
        var height = settings.WindowHeight;

        // Ensure window position is valid (at least 50% visible on primary monitor)
        var (validX, validY) = MonitorHelper.EnsureValidWindowPosition(left, top, width, height);
        
        // Check if we had to adjust the position (window was off-screen)
        if (validX != (int)left || validY != (int)top)
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
