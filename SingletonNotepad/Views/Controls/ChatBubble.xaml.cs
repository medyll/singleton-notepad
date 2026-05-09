using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SingletonNotepad.ViewModels;

namespace SingletonNotepad.Views.Controls;

public sealed partial class ChatBubble : UserControl
{
    public ChatViewModel ViewModel { get; }

    public ChatBubble(ChatViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        UpdateVisibility();
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.PanelState))
                UpdateVisibility();
        };
    }

    private void UpdateVisibility()
    {
        MinimizedButton.Visibility = ViewModel.IsMinimized ? Visibility.Visible : Visibility.Collapsed;
        ChatExpanded.Visibility = ViewModel.IsOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnMinimizedClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenPanelCommand.Execute(null);
        UpdateVisibility();
    }

    private void OnToggleClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.TogglePanelCommand.Execute(null);
        UpdateVisibility();
    }

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter || ViewModel.IsSending) return;
        var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        var ctrlDown = (ctrl & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        if (!ctrlDown) return;
        ViewModel.SendCommand.Execute(null);
        e.Handled = true;
    }

    private void OnSendClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.SendCommand.Execute(null);
    }

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearChatCommand.Execute(null);
    }
}
