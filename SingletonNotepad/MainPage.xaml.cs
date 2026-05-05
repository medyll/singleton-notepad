using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad;

public sealed partial class MainPage : Page
{
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
                var showDiff = ViewModel.IsShowingDiff;
                DiffOverlay.Visibility = showDiff ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
                EditorTextBox.Visibility = showDiff ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
                MarkdownPreviewControl.Visibility = showDiff ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
            }
            if (args.PropertyName == nameof(ViewModel.IsSyntaxHighlightEnabled))
            {
                UpdateEditorVisibility();
            }
        };

        SyntaxToggle.Checked += (_, _) =>
        {
            ViewModel.IsSyntaxHighlightEnabled = true;
            UpdateEditorVisibility();
        };
        SyntaxToggle.Unchecked += (_, _) =>
        {
            ViewModel.IsSyntaxHighlightEnabled = false;
            UpdateEditorVisibility();
        };
    }

    private void UpdateEditorVisibility()
    {
        if (ViewModel.IsShowingDiff)
        {
            EditorTextBox.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            MarkdownPreviewControl.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }

        if (ViewModel.IsSyntaxHighlightEnabled)
        {
            EditorTextBox.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            MarkdownPreviewControl.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else
        {
            EditorTextBox.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            MarkdownPreviewControl.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }
}