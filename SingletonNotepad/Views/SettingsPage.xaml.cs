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
        // Select first nav item by default
        SettingsNavView.SelectedItem = SettingsNavView.MenuItems[0];
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag as string;
            AppearancePane.Visibility = tag == "appearance"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            FilesPane.Visibility = tag == "files"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            HistoryPane.Visibility = tag == "history"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            ApiKeysPane.Visibility = tag == "apikeys"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            RulesPane.Visibility = tag == "rules"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
            NormalizationPane.Visibility = tag == "normalization"
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;

            if (tag == "history")
            {
                _ = ViewModel.LoadBackupsAsync();
            }
            else if (tag == "rules")
            {
                _ = ViewModel.LoadRulesAsync();
            }
        }
    }

    private async void OnSaveRules(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveRulesAsync();
    }

    private async void OnRefreshBackups(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadBackupsAsync();
    }

    private async void OnRestoreBackup(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            await ViewModel.RestoreBackupAsync(path);
        }
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        var frame = MainWindow.GetRootFrame();
        if (frame?.CanGoBack == true)
        {
            frame.GoBack();
        }
    }
}
