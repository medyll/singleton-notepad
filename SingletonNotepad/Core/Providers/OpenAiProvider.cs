using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Providers;

public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private const string DefaultEndpoint = "https://api.openai.com/v1";
    private const string DefaultModel    = "gpt-4o-mini";

    public string Name => "OpenAI";

    public OpenAiProvider(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var apiKey = await _settingsService.UnprotectApiKeyAsync(settings.OpenAiApiKey ?? string.Empty, ct);

        var request = new OpenAiChatRequest
        {
            Model = settings.OpenAiModel,
            Messages =
            [
                new OpenAiMessage { Role = "system", Content = systemPrompt },
                new OpenAiMessage { Role = "user",   Content = userMessage  },
            ],
            MaxTokens = 2048,
            Temperature = 0.3f,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{DefaultEndpoint}/chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cts.Token);
        return result?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }

    public async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var apiKey = await _settingsService.UnprotectApiKeyAsync(settings.OpenAiApiKey ?? string.Empty, ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{DefaultEndpoint}/models");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiModelsResponse>(cancellationToken: cts.Token);
        return result?.Data
            .Select(m => m.Id)
            .Where(id => id.StartsWith("gpt-") || id.StartsWith("o"))
            .OrderBy(id => id)
            .ToList() ?? [];
    }
}

internal class OpenAiChatRequest
{
    [JsonPropertyName("model")]       public string          Model       { get; set; } = string.Empty;
    [JsonPropertyName("messages")]    public OpenAiMessage[] Messages    { get; set; } = [];
    [JsonPropertyName("max_tokens")]  public int             MaxTokens   { get; set; }
    [JsonPropertyName("temperature")] public float           Temperature { get; set; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("role")]    public string Role    { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

internal class OpenAiChatResponse
{
    [JsonPropertyName("choices")] public OpenAiChoice[] Choices { get; set; } = [];
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")] public OpenAiMessage Message { get; set; } = new();
}

internal class OpenAiModelsResponse
{
    [JsonPropertyName("data")] public OpenAiModelEntry[] Data { get; set; } = [];
}

internal class OpenAiModelEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
}
