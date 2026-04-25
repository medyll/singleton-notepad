using Microsoft.UI.Xaml.Controls;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public sealed partial class ApparenceSettingsPage : Page
{
    public SettingsViewModel ViewModel { get; private set; } = null!;

    public ApparenceSettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (SettingsViewModel)e.Parameter;
        DataContext = ViewModel;
    }
}
