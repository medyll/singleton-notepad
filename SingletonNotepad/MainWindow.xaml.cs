using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SingletonNotepad.Core.Helpers;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;
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

    private async Task RestoreWindowPositionAsync()
    {
        try
        {
            var settings = await _settingsService.LoadAsync();
            var geo = settings.WindowGeometry;

            if (geo.Width > 0 && geo.Height > 0)
            {
                // Validate position is on primary monitor
                if (MonitorHelper.IsWindowValidOnPrimaryMonitor(geo.X, geo.Y, geo.Width, geo.Height))
                {
                    AppWindow.Move(new PointInt32(geo.X, geo.Y));
                    AppWindow.Resize(new SizeInt32(geo.Width, geo.Height));
                }
                else
                {
                    // Fallback: center on primary monitor
                    var (x, y) = MonitorHelper.CenterOnPrimaryMonitor(geo.Width, geo.Height);
                    AppWindow.Move(new PointInt32(x, y));
                    AppWindow.Resize(new SizeInt32(geo.Width, geo.Height));
                }
            }
            else
            {
                // First run: default size, centered
                const int defaultW = 1200;
                const int defaultH = 800;
                var (x, y) = MonitorHelper.CenterOnPrimaryMonitor(defaultW, defaultH);
                AppWindow.Move(new PointInt32(x, y));
                AppWindow.Resize(new SizeInt32(defaultW, defaultH));
            }
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
        catch
        {
            // Best-effort persistence
        }
    }
}
