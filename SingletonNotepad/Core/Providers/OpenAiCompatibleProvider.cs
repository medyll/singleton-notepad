using System.Net.Http.Json;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Providers;

public class OpenAiCompatibleProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly string _defaultModel;

    public string Name { get; }

    public OpenAiCompatibleProvider(
        string name,
        string baseUrl,
        string apiKey,
        string defaultModel,
        HttpClient httpClient,
        ISettingsService settingsService)
    {
        Name = name;
        _baseUrl = baseUrl;
        _apiKey = apiKey;
        _defaultModel = defaultModel;
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var model = settings.ProviderModels.TryGetValue(Name, out var m) && !string.IsNullOrEmpty(m)
            ? m
            : _defaultModel;

        var request = new OpenAiChatRequest
        {
            Model = model,
            Messages =
            [
                new OpenAiMessage { Role = "system", Content = systemPrompt },
                new OpenAiMessage { Role = "user",   Content = userMessage  },
            ],
            MaxTokens = 2048,
            Temperature = 0.3f,
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken: cts.Token);
        return result?.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }
}
