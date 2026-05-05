using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
        Loaded += async (_, _) => await ViewModel.LoadContentAsync();

        DiffViewer.ApplyClicked += async (_, _) => await ViewModel.ApplyNormalizationCommand.ExecuteAsync(null);
        DiffViewer.CancelClicked += (_, _) => ViewModel.CancelNormalizationCommand.Execute(null);

        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.PendingNormalization) && ViewModel.PendingNormalization != null)
            {
                DiffViewer.SetDiff(ViewModel.PendingNormalization.Diff);
            }
            if (args.PropertyName == nameof(ViewModel.IsShowingDiff))
            {
                DiffOverlay.Visibility = ViewModel.IsShowingDiff
                    ? Microsoft.UI.Xaml.Visibility.Visible
                    : Microsoft.UI.Xaml.Visibility.Collapsed;
                EditorTextBox.Visibility = ViewModel.IsShowingDiff
                    ? Microsoft.UI.Xaml.Visibility.Collapsed
                    : Microsoft.UI.Xaml.Visibility.Visible;
            }
        };
    }
}
}
