using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using SingletonNotepad.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace SingletonNotepad;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    private readonly TaskCompletionSource<bool> _editorReady = new();
    private bool _updatingFromWebView;
    private bool _editorInitialized;
    private bool _pushingContentToWebView;

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();

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

        await EditorWebView.EnsureCoreWebView2Async();

        var assetPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        EditorWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "editor.local", assetPath,
            CoreWebView2HostResourceAccessKind.Allow);

        EditorWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        EditorWebView.CoreWebView2.Navigate("https://editor.local/editor.html");

        // Wait for TipTap ready with timeout so a missed signal doesn't freeze load
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _editorReady.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[WebView2] Timed out waiting for editor ready — loading content anyway");
            _editorReady.TrySetResult(true);
        }

        SyncTheme();
        await ViewModel.LoadContentAsync();
        PushContentToEditor(ViewModel.EditorContent);
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var json = args.TryGetWebMessageAsString();
        if (json is null) return;

        EditorMessage? msg;
        try { msg = JsonSerializer.Deserialize<EditorMessage>(json); }
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
        ActualThemeChanged += (_, _) => SyncTheme();
    }

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
