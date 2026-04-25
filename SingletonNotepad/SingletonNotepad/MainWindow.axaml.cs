using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using SingletonNotepad.Core.Services;
using SingletonNotepad.Views;

namespace SingletonNotepad;

public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly MainView _mainView;
    private readonly SettingsView _settingsView;
    private readonly NotificationService _notificationService;

    public MainWindow(
        ISettingsService settingsService,
        MainView mainView,
        SettingsView settingsView,
        NotificationService notificationService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _mainView = mainView;
        _settingsView = settingsView;
        _notificationService = notificationService;

        Content = _mainView;

        _mainView.RequestSettings += OnRequestSettings;
        _settingsView.ViewModel.RequestClose += OnRequestCloseSettings;

        this.Opened += OnOpened;
        this.Closing += OnClosing;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var manager = new WindowNotificationManager(this);
        _notificationService.SetManager(manager);

        RestoreWindowPosition();
    }

    private void OnRequestSettings(object? sender, EventArgs e)
    {
        Content = _settingsView;
    }

    private void OnRequestCloseSettings(object? sender, EventArgs e)
    {
        Content = _mainView;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        RestoreWindowPosition();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (WindowState != WindowState.Normal) return;

        var bounds = Bounds;

        if (bounds.Width < 400 || bounds.Height < 300) return;

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

        if (width < 400 || height < 300) return;

        Position = new PixelPoint((int)left, (int)top);
        Width = width;
        Height = height;
    }
}
