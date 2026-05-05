using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SingletonNotepad.Core.Providers;

public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public string Name => "Anthropic";

    public AnthropicProvider(HttpClient httpClient, string model = "claude-3-5-haiku-20240620")
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var request = new AnthropicMessageRequest
        {
            Model = _model,
            MaxTokens = 2048,
            Messages = new[]
            {
                new AnthropicMessage { Role = "user", Content = prompt }
            },
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await _httpClient.PostAsJsonAsync("https://api.anthropic.com/v1/messages", request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }, cts.Token);
        return result?.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
}

internal class AnthropicMessageRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("messages")]
    public AnthropicMessage[] Messages { get; set; } = Array.Empty<AnthropicMessage>();
}

internal class AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class AnthropicMessageResponse
{
    [JsonPropertyName("content")]
    public AnthropicContentBlock[] Content { get; set; } = Array.Empty<AnthropicContentBlock>();
}

internal class AnthropicContentBlock
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}