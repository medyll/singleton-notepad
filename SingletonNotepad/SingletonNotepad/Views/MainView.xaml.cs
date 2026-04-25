using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

/// <summary>
/// Main view with MenuBar, CommandBar, Editor, and StatusBar.
/// </summary>
public sealed partial class MainView : UserControl
{
    public MainViewModel ViewModel { get; }
    
    public event EventHandler? RequestSettings;

    public MainView(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
    
    private void OnSettingsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RequestSettings?.Invoke(this, EventArgs.Empty);
    }
}
