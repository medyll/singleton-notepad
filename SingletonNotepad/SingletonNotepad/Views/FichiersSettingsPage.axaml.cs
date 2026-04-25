using Avalonia.Controls;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public partial class FichiersSettingsPage : UserControl
{
    public SettingsViewModel? ViewModel
    {
        get => DataContext as SettingsViewModel;
        set => DataContext = value;
    }

    public FichiersSettingsPage()
    {
        InitializeComponent();
    }
}
