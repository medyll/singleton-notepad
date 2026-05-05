using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SingletonNotepad.Core.Providers;

public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _model;

    public string Name => "OpenAI";

    public OpenAiProvider(HttpClient httpClient, string apiKey, string endpoint = "https://api.openai.com/v1", string model = "gpt-4o-mini")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _endpoint = endpoint.TrimEnd('/');
        _model = model;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var request = new OpenAiChatRequest
        {
            Model = _model,
            Messages = new[]
            {
                new OpenAiMessage { Role = "user", Content = prompt }
            },
            MaxTokens = 2048,
            Temperature = 0.7f,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cts.Token);
        return result?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }
}

internal class OpenAiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public OpenAiMessage[] Messages { get; set; } = Array.Empty<OpenAiMessage>();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class OpenAiChatResponse
{
    [JsonPropertyName("choices")]
    public OpenAiChoice[] Choices { get; set; } = Array.Empty<OpenAiChoice>();
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage Message { get; set; } = new();
}