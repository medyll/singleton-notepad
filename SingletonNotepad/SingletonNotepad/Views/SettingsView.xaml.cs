using Microsoft.UI.Xaml.Controls;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        
        // Select Apparence by default
        SettingsNavView.SelectedItem = SettingsNavView.MenuItems[0];
    }

    private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)sender.SelectedItem;
        var tag = selectedItem?.Tag as string;

        switch (tag)
        {
            case "Apparence":
                SettingsContentFrame.Navigate(typeof(ApparenceSettingsPage), ViewModel);
                break;
            case "Fichiers":
                SettingsContentFrame.Navigate(typeof(FichiersSettingsPage), ViewModel);
                break;
        }
    }
}
