using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SingletonNotepad.Core.Helpers;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;
using System.Text.Json;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SingletonNotepad;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        _settingsService = App.Services.GetRequiredService<ISettingsService>();

        // Restore window position from settings
        _ = RestoreWindowPositionAsync();

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
        RootFrame.Navigated += (_, _) =>
            AppTitleBar.IsBackButtonVisible = RootFrame.CanGoBack;

        // Persist position on close
        Closed += OnClosed;
    }

    /// <summary>
    /// Navigate the root frame to the specified page type.
    /// Exposed for ViewModels that need to trigger navigation.
    /// </summary>
    public void NavigateToPage(Type pageType)
    {
        RootFrame.Navigate(pageType);
    }

    /// <summary>
    /// Get the root frame for navigation operations.
    /// </summary>
    public static Frame? GetRootFrame()
    {
        return (App.Window as MainWindow)?.RootFrame;
    }

    private void OnTitleBarBackRequested(TitleBar sender, object args)
    {
        if (RootFrame.CanGoBack)
        {
            RootFrame.GoBack();
            AppTitleBar.IsBackButtonVisible = RootFrame.CanGoBack;
        }
    }

    private async Task RestoreWindowPositionAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            var geo = settings.WindowGeometry;

            var w = geo.Width > 0 ? geo.Width : 1200;
            var h = geo.Height > 0 ? geo.Height : 800;

            int x, y;
            if (settings.AlwaysStartAtBottom)
            {
                (x, y) = MonitorHelper.BottomCenterOnPrimaryMonitor(w, h);
            }
            else if (geo.Width > 0 && MonitorHelper.IsWindowValidOnPrimaryMonitor(geo.X, geo.Y, w, h))
            {
                x = geo.X;
                y = geo.Y;
            }
            else
            {
                (x, y) = MonitorHelper.CenterOnPrimaryMonitor(w, h);
            }

            AppWindow.Move(new PointInt32(x, y));
            AppWindow.Resize(new SizeInt32(w, h));

            if (settings.AlwaysOnTop && AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                p.IsAlwaysOnTop = true;
        }
        catch
        {
            // If anything fails, window stays at default position
        }
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            var pos = AppWindow.Position;
            var size = AppWindow.Size;

            settings.WindowGeometry = new WindowGeometry
            {
                X = pos.X,
                Y = pos.Y,
                Width = size.Width,
                Height = size.Height,
            };

            await _settingsService.SaveAsync(settings);
        }
        catch (IOException)
        {
            // Best-effort persistence — disk busy
        }
        catch (JsonException)
        {
            // Best-effort persistence — serialization error
        }
    }
}
