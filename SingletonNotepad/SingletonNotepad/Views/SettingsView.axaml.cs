using Avalonia.Controls;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public partial class SettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }
}
