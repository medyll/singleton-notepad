using Avalonia.Controls;
using Avalonia.Interactivity;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views;

public partial class MainView : UserControl
{
    public MainViewModel ViewModel { get; }

    public event EventHandler? RequestSettings;

    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        RequestSettings?.Invoke(this, EventArgs.Empty);
    }
}
