using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SingletonNotepad.Core.Services;
using SingletonNotepad.ViewModels;
using SingletonNotepad.Views;

namespace SingletonNotepad;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    private Window? _mainWindow;
    private AppInstance? _currentAppInstance;

    public App()
    {
        // Single-instance guard BEFORE InitializeComponent (Win App SDK requirement)
        _currentAppInstance = AppInstance.FindOrRegisterForKey("singleton-notepad");
        if (!_currentAppInstance.IsCurrent)
        {
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            _currentAppInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            Environment.Exit(0);
            return;
        }

        // Register bring-to-front handler for subsequent launch attempts
        _currentAppInstance.Activated += OnInstanceActivated;

        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        this.UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainWindow.Activate();

        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        _ = viewModel.LoadFileCommand.ExecuteAsync(null);
    }

    private void OnInstanceActivated(object? sender, AppActivationArguments args)
    {
        // Second instance launched — bring existing window to front on the UI thread
        _mainWindow?.DispatcherQueue.TryEnqueue(BringWindowToFront);
    }

    private void BringWindowToFront()
    {
        if (_mainWindow is null) return;
        var hwnd = WinUI.Interop.GetWindowHandle(_mainWindow);
        WinUI.Interop.ShowWindow(hwnd, WinUI.Interop.SW_RESTORE);
        WinUI.Interop.SetForegroundWindow(hwnd);
        _mainWindow.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<IFileService, FileService>();
        services.AddTransient<INormalizationService, NormalizationService>();
        services.AddTransient<IMemoryTrackerService, MemoryTrackerService>();
        services.AddTransient<INotificationService, NotificationService>();

        // LLM Providers registered by name for strategy pattern (Sprint 2+)
        // services.AddTransient<ILlmProvider, OllamaProvider>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<ApparenceSettingsPage>();
        services.AddTransient<FichiersSettingsPage>();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");
        e.Handled = true;
    }
}
