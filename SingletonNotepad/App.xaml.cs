using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;
using SingletonNotepad.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SingletonNotepad;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private const string MutexName = "SingletonNotepad-7B3F9A2C-Instance";
    private static Mutex? _mutex;

    /// <summary>
    /// The main application window. Use <c>App.Window</c> from any class that needs
    /// the window reference (for dialogs, pickers, interop, etc.).
    /// </summary>
    public static Window Window { get; private set; } = null!;

    /// <summary>
    /// The UI thread dispatcher. Use <c>App.DispatcherQueue</c> to marshal calls
    /// to the UI thread. Fully qualified to avoid CS0104 ambiguity with
    /// <see cref="Windows.System.DispatcherQueue"/>.
    /// </summary>
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    /// <summary>
    /// The native window handle (HWND). Use for file pickers,
    /// <c>DataTransferManager</c>, and any WinRT interop that requires
    /// <c>InitializeWithWindow</c>.
    /// </summary>
    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    /// <summary>
    /// The DI service provider. Use <c>App.Services</c> to resolve services.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single-instance enforcement via named mutex
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            BringExistingWindowToFront();
            Current.Exit();
            return;
        }

        // Build DI container
        Services = ConfigureServices();

        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Window.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // HTTP client
        services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(35) });

        // Services (Sprint 1)
        var settingsService = new SettingsService();
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddSingleton<IFileService, FileService>();

        // Services (Sprint 2)
        services.AddSingleton<INormalizationService, NormalizationService>();
        services.AddSingleton<IMemoryTrackerService, MemoryTrackerService>();

        // Providers — ISettingsService injected; keys read lazily at call time (no .Result blocking)
        services.AddSingleton<OllamaProvider>();
        services.AddSingleton<OpenAiProvider>();
        services.AddSingleton<AnthropicProvider>();
        services.AddSingleton<ILlmProvider, OllamaProvider>();
        services.AddSingleton<ILlmProviderSelector, LlmProviderSelector>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private static void BringExistingWindowToFront()
    {
        try
        {
            // Find the existing window by title
            var hwnd = FindWindow(null, "SingletonNotepad");
            if (hwnd != nint.Zero)
            {
                // If minimized, restore it
                if (IsIconic(hwnd))
                {
                    ShowWindow(hwnd, SW_RESTORE);
                }
                else
                {
                    ShowWindow(hwnd, SW_SHOW);
                }
                SetForegroundWindow(hwnd);
            }
        }
        catch
        {
            // Best-effort; don't crash if interop fails
        }
    }

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);
}
