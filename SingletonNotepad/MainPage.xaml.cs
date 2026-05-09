using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using SingletonNotepad.Core.Services;
using SingletonNotepad.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace SingletonNotepad;

public sealed partial class MainPage : Page
{
    private static readonly JsonSerializerOptions MessageJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MainViewModel ViewModel { get; }
    public ChatViewModel ChatViewModel { get; }

    private readonly TaskCompletionSource<bool> _editorReady = new();
    private readonly Task _webView2InitTask;
    private readonly Task _loadContentTask;
    private bool _updatingFromWebView;
    private bool _editorInitialized;
    private bool _pushingContentToWebView;
    private bool _themeHandlerWired;
    private SingletonNotepad.Views.Controls.ChatBubble? _chatBubbleControl;

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        ChatViewModel = App.Services.GetRequiredService<ChatViewModel>();
        InitializeComponent();

        _chatBubbleControl = new SingletonNotepad.Views.Controls.ChatBubble(ChatViewModel);
        var rootGrid = (Grid)Content;
        Grid.SetRowSpan(_chatBubbleControl, 3);
        rootGrid.Children.Add(_chatBubbleControl);

        ChatViewModel.SetContentProviders(
            () => null, // No selection detection from WebView2 yet
            () => ViewModel.EditorContent);

        _ = ChatViewModel.LoadStateAsync();

        // Kick off both heavy tasks immediately — before Loaded fires
        _webView2InitTask = EditorWebView.EnsureCoreWebView2Async().AsTask();
        _loadContentTask = ViewModel.LoadContentAsync();

        DiffViewer.ApplyClicked += async (_, _) =>
        {
            await ViewModel.ApplyNormalizationCommand.ExecuteAsync(null);
            HideDiff();
        };

        DiffViewer.CancelClicked += (_, _) =>
        {
            ViewModel.CancelNormalizationCommand.Execute(null);
            HideDiff();
        };

        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.PendingNormalization) && ViewModel.PendingNormalization != null)
            {
                DiffViewer.SetDiff(ViewModel.PendingNormalization.Diff);
                ShowDiff();
            }
            else if (args.PropertyName == nameof(ViewModel.EditorContent) && !_updatingFromWebView)
            {
                PushContentToEditor(ViewModel.EditorContent);
            }
        };
    }

    private async void OnEditorWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_editorInitialized) return;
        _editorInitialized = true;

        // Already started in constructor — just await completion
        await _webView2InitTask;

        var assetPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        EditorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "editor.local", assetPath,
            CoreWebView2HostResourceAccessKind.Allow);

        EditorWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        EditorWebView.CoreWebView2.Navigate("https://editor.local/editor.html");

        // Wait for TipTap ready and file load — both already running since constructor
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await Task.WhenAll(_loadContentTask, _editorReady.Task.WaitAsync(cts.Token));
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[WebView2] Timed out waiting for editor ready — loading content anyway");
            _editorReady.TrySetResult(true);
            await _loadContentTask;
        }

        SyncTheme();
        PushContentToEditor(ViewModel.EditorContent);
        _ = ApplySpellCheckSettingsAsync();
    }

    private async Task ApplySpellCheckSettingsAsync()
    {
        var settings = await App.Services.GetRequiredService<ISettingsService>().LoadAsync();
        if (!settings.SpellCheckEnabled)
        {
            SendSpellCheckMessage(false, null);
            return;
        }

        var lang = settings.SpellCheckLanguage?.Trim().ToLowerInvariant();
        if (lang == "auto")
        {
            var content = ViewModel.EditorContent ?? string.Empty;
            var detected = LanguageDetector.Detect(content);
            lang = detected == "auto" ? "fr-FR" : detected;
        }

        SendSpellCheckMessage(true, lang);
    }

    private void SendSpellCheckMessage(bool enabled, string? lang)
    {
        var payload = JsonSerializer.Serialize(new { type = "setSpellCheck", enabled, lang });
        EditorWebView.CoreWebView2?.PostWebMessageAsString(payload);
        ViewModel.SpellCheckLang = enabled && !string.IsNullOrEmpty(lang) ? lang : "";
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var json = args.TryGetWebMessageAsString();
        if (json is null) return;

        EditorMessage? msg;
        try { msg = JsonSerializer.Deserialize<EditorMessage>(json, MessageJsonOptions); }
        catch { return; }

        if (msg is null) return;

        if (msg.Type == "ready")
        {
            // TCS.TrySetResult is thread-safe — no need to dispatch
            _editorReady.TrySetResult(true);
            return;
        }

        App.DispatcherQueue.TryEnqueue(() =>
        {
            if (msg.Type == "change")
            {
                if (_pushingContentToWebView) return;
                _updatingFromWebView = true;
                ViewModel.EditorContent = msg.Content ?? string.Empty;
                _updatingFromWebView = false;
            }
        });
    }

    private void PushContentToEditor(string content)
    {
        if (!_editorReady.Task.IsCompleted) return;
        _pushingContentToWebView = true;
        var escaped = JsonSerializer.Serialize(content);
        var op = EditorWebView.CoreWebView2?.ExecuteScriptAsync($"window.__setContent({escaped})");
        if (op is not null)
            _ = op.AsTask().ContinueWith(_ => { _pushingContentToWebView = false; });
        else
            _pushingContentToWebView = false;
    }

    private void SyncTheme()
    {
        var isDark = ActualTheme == ElementTheme.Dark;
        var payload = JsonSerializer.Serialize(new { type = "setTheme", dark = isDark });
        EditorWebView.CoreWebView2?.PostWebMessageAsString(payload);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_themeHandlerWired) return;
        _themeHandlerWired = true;
        ActualThemeChanged += OnActualThemeChanged;
        KeyDown += OnPageKeyDown;
    }

    private void OnPageKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
        var ctrlDown = (ctrl & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        var shiftDown = (shift & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        if (e.Key == Windows.System.VirtualKey.C && ctrlDown && shiftDown)
        {
            ChatViewModel.TogglePanelCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args) => SyncTheme();

    private void ShowDiff()
    {
        DiffOverlay.Visibility = Visibility.Visible;
    }

    private void HideDiff()
    {
        DiffOverlay.Visibility = Visibility.Collapsed;
        EditorWebView.CoreWebView2?.PostWebMessageAsString(
            JsonSerializer.Serialize(new { type = "focus" }));
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        EditorWebView.CoreWebView2?.PostWebMessageAsString(
            JsonSerializer.Serialize(new { type = "selectAll" }));
    }

    private async void OnPasteClicked(object sender, RoutedEventArgs e)
    {
        var view = Clipboard.GetContent();
        if (!view.Contains(StandardDataFormats.Text)) return;
        var text = await view.GetTextAsync();
        var payload = JsonSerializer.Serialize(new { type = "paste", text });
        EditorWebView.CoreWebView2?.PostWebMessageAsString(payload);
    }

    private sealed record EditorMessage(string? Type, string? Content);
}
