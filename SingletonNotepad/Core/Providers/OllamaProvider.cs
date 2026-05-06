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

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var endpoint = (settings.OllamaEndpoint ?? "http://localhost:11434").TrimEnd('/');
        var model = settings.OllamaModel ?? "llama3";

        var request = new OllamaGenerateRequest { Model = model, Prompt = prompt, Stream = false };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await _httpClient.PostAsJsonAsync($"{endpoint}/api/generate", request, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cts.Token);
        return result?.Response ?? string.Empty;
    }
}

internal class OllamaGenerateRequest
{
    [JsonPropertyName("model")]   public string Model  { get; set; } = string.Empty;
    [JsonPropertyName("prompt")]  public string Prompt { get; set; } = string.Empty;
    [JsonPropertyName("stream")]  public bool   Stream { get; set; }
}

internal class OllamaGenerateResponse
{
    [JsonPropertyName("response")] public string Response { get; set; } = string.Empty;
}
