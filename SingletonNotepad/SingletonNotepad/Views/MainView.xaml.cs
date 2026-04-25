using Microsoft.UI.Xaml.Controls;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

/// <summary>
/// Main view with MenuBar, CommandBar, Editor, and StatusBar.
/// </summary>
public sealed partial class MainView : UserControl
{
    public MainViewModel ViewModel { get; }

    public MainView(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
