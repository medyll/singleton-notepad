using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadSettingsAsync();
        await ViewModel.LoadRulesAsync();
        await ViewModel.LoadBackupsAsync();
    }

    private async void OnSaveRules(object sender, RoutedEventArgs e)
        => await ViewModel.SaveRulesAsync();

    private async void OnRefreshBackups(object sender, RoutedEventArgs e)
        => await ViewModel.LoadBackupsAsync();

    private async void OnRestoreBackup(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
            await ViewModel.RestoreBackupAsync(path);
    }
}
