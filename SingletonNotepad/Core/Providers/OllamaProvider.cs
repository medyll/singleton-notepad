using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Providers;

public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    public string Name => "Ollama";

    public OllamaProvider(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var endpoint = (settings.OllamaEndpoint ?? "http://localhost:11434").TrimEnd('/');
        var model = settings.OllamaModel ?? "qwen3.5:latest";

        var request = new OllamaChatRequest
        {
            Model  = model,
            Stream = false,
            Messages =
            [
                new OllamaChatMessage { Role = "system", Content = systemPrompt },
                new OllamaChatMessage { Role = "user",   Content = userMessage  },
            ],
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(120));

        var response = await _httpClient.PostAsJsonAsync($"{endpoint}/api/chat", request, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cts.Token);
        return result?.Message?.Content ?? string.Empty;
    }
}

internal class OllamaChatRequest
{
    [JsonPropertyName("model")]    public string             Model    { get; set; } = string.Empty;
    [JsonPropertyName("stream")]   public bool               Stream   { get; set; }
    [JsonPropertyName("messages")] public OllamaChatMessage[] Messages { get; set; } = [];
}

internal class OllamaChatMessage
{
    [JsonPropertyName("role")]    public string Role    { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

internal class OllamaChatResponse
{
    [JsonPropertyName("message")] public OllamaChatMessage? Message { get; set; }
}
