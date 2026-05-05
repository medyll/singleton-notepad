using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using SingletonNotepad.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SingletonNotepad;

/// <summary>
/// The main content page displayed inside the application window.
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>
    /// Typed ViewModel property — required for x:Bind to compile.
    /// Resolved from DI in the constructor.
    /// </summary>
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }
}
