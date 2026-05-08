using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Providers;

public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private const string Endpoint      = "https://api.anthropic.com/v1/messages";
    private const string DefaultModel  = "claude-haiku-4-5-20251001";
    private const string ApiVersion    = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public string Name => "Anthropic";

    public AnthropicProvider(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var apiKey = await _settingsService.UnprotectApiKeyAsync(settings.AnthropicApiKey ?? string.Empty, ct);

        var request = new AnthropicMessageRequest
        {
            Model     = settings.AnthropicModel,
            MaxTokens = 2048,
            System    = systemPrompt,
            Messages  = [new AnthropicMessage { Role = "user", Content = userMessage }],
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOpts),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(JsonOpts, cts.Token);
        return result?.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
}

internal class AnthropicMessageRequest
{
    [JsonPropertyName("model")]      public string             Model     { get; set; } = string.Empty;
    [JsonPropertyName("max_tokens")] public int                MaxTokens { get; set; }
    [JsonPropertyName("system")]     public string             System    { get; set; } = string.Empty;
    [JsonPropertyName("messages")]   public AnthropicMessage[] Messages  { get; set; } = [];
}

internal class AnthropicMessage
{
    [JsonPropertyName("role")]    public string Role    { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

internal class AnthropicMessageResponse
{
    [JsonPropertyName("content")] public AnthropicContentBlock[] Content { get; set; } = [];
}

internal class AnthropicContentBlock
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
}
