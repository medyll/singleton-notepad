using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;
using SingletonNotepad.ViewModels;
using SingletonNotepad.Views;

namespace SingletonNotepad;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _mainWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// Bootstraps DI container and enforces single-instance.
    /// </summary>
    public App()
    {
        // Bootstrap DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Single-instance enforcement
        var appInstance = AppInstance.FindOrRegisterForKey("singleton-notepad");
        if (!appInstance.IsCurrent)
        {
            // Redirect activation to existing instance
            appInstance.RedirectActivationToAsync(new AppActivationArguments
            {
                Data = "BringToFront"
            });
            Environment.Exit(0);
            return;
        }

        this.UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
        
        // Load file after window is shown
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        _ = viewModel.LoadFileCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Configures all services in the DI container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Services - Singleton for settings, Transient for others
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<IFileService, FileService>();
        services.AddTransient<INormalizationService, NormalizationService>();
        services.AddTransient<IMemoryTrackerService, MemoryTrackerService>();
        services.AddTransient<INotificationService, NotificationService>();
        
        // LLM Providers - registered by name for strategy pattern
        // services.AddTransient<ILlmProvider, OllamaProvider>("Ollama"); // Sprint 2
        // services.AddTransient<ILlmProvider, OpenAiProvider>("OpenAI"); // Sprint 3
        // services.AddTransient<ILlmProvider, AnthropicProvider>("Anthropic"); // Sprint 3
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log unhandled exceptions
        System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");
        e.Handled = true;
    }
}
