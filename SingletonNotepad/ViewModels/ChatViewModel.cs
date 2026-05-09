using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private readonly ISettingsService _settingsService;
    private Func<string?>? _getSelectedContent;
    private Func<string>? _getFullContent;

    [ObservableProperty]
    public partial ObservableCollection<ChatMessage> Messages { get; set; } = new();

    [ObservableProperty]
    public partial string UserInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    public partial string PanelState { get; set; } = "minimized";

    [ObservableProperty]
    public partial string LastResponse { get; set; } = string.Empty;

    public bool IsOpen => PanelState == "open";
    public bool IsMinimized => PanelState == "minimized";

    public ChatViewModel(IChatService chatService, ISettingsService settingsService)
    {
        _chatService = chatService;
        _settingsService = settingsService;
    }

    public void SetContentProviders(Func<string?> getSelectedContent, Func<string> getFullContent)
    {
        _getSelectedContent = getSelectedContent;
        _getFullContent = getFullContent;
    }

    public async Task LoadStateAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        PanelState = settings.ChatBubbleState;
    }

    private async Task SaveStateAsync()
    {
        var settings = await _settingsService.LoadAsync();
        settings.ChatBubbleState = PanelState;
        await _settingsService.SaveAsync(settings);
    }

    partial void OnPanelStateChanged(string value)
    {
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsMinimized));
        _ = SaveStateAsync();
    }

    [RelayCommand]
    public async Task SendAsync()
    {
        if (IsSending || string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMsg = new ChatMessage { Role = "user", Content = UserInput };
        Messages.Add(userMsg);

        var input = UserInput;
        UserInput = string.Empty;
        IsSending = true;

        try
        {
            var context = GetContextContent();
            var response = await _chatService.SendAsync(input, context);

            var assistantMsg = new ChatMessage { Role = "assistant", Content = response };
            Messages.Add(assistantMsg);
            LastResponse = response;
        }
        catch (Exception ex)
        {
            var errorMsg = new ChatMessage { Role = "assistant", Content = $"Erreur: {ex.Message}" };
            Messages.Add(errorMsg);
        }
        finally
        {
            IsSending = false;
        }
    }

    private string? GetContextContent()
    {
        var selected = _getSelectedContent?.Invoke();
        if (!string.IsNullOrWhiteSpace(selected))
            return selected;
        return _getFullContent?.Invoke();
    }

    [RelayCommand]
    public void OpenPanel()
    {
        PanelState = "open";
    }

    [RelayCommand]
    public void MinimizePanel()
    {
        PanelState = "minimized";
    }

    [RelayCommand]
    public void TogglePanel()
    {
        PanelState = PanelState == "open" ? "minimized" : "open";
    }

    [RelayCommand]
    public void ClearChat()
    {
        Messages.Clear();
        LastResponse = string.Empty;
    }
}
