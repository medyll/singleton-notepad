using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SingletonNotepad.Core.Providers;

public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _model;

    public string Name => "Ollama";

    public OllamaProvider(HttpClient httpClient, string endpoint = "http://localhost:11434", string model = "llama3")
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
        _model = model;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var request = new OllamaGenerateRequest
        {
            Model = _model,
            Prompt = prompt,
            Stream = false,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/generate", request, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cts.Token);
        return result?.Response ?? string.Empty;
    }
}

internal class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

internal class OllamaGenerateResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
