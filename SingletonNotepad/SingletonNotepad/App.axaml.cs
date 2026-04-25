using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SingletonNotepad.Core.Services;
using SingletonNotepad.ViewModels;
using SingletonNotepad.Views;

namespace SingletonNotepad;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<IFileService, FileService>();
        services.AddTransient<INormalizationService, NormalizationService>();
        services.AddTransient<IMemoryTrackerService, MemoryTrackerService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NotificationService>());

        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<ApparenceSettingsPage>();
        services.AddTransient<FichiersSettingsPage>();
    }
}
