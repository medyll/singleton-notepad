using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Tests;

[TestClass]
public class OpenAiProviderTests
{
    private static OpenAiProvider Make(HttpClient http, string apiKey = "sk-test-key")
        => new(http, new StubSettingsService(new AppSettings { OpenAiApiKey = apiKey }));

    [TestMethod]
    public void Name_ReturnsOpenAi()
    {
        Assert.AreEqual("OpenAI", Make(new HttpClient()).Name);
    }

    [TestMethod]
    public async Task CompleteAsync_SendsCorrectRequest()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(
            "https://api.openai.com/v1/chat/completions",
            new { choices = new[] { new { message = new { role = "assistant", content = "Normalized markdown content" } } } });

        var result = await Make(new HttpClient(mockHandler)).CompleteAsync("Fix my markdown");

        Assert.AreEqual("Normalized markdown content", result);
        var sentRequest = mockHandler.SentRequests.First();
        Assert.IsTrue(sentRequest.Content.Contains("gpt-4o-mini"));
        Assert.IsTrue(sentRequest.Content.Contains("Fix my markdown"));
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenNoChoices()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(
            "https://api.openai.com/v1/chat/completions",
            new { choices = Array.Empty<object>() });

        var result = await Make(new HttpClient(mockHandler)).CompleteAsync("test");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task CompleteAsync_Throws_OnHttpError()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupError("https://api.openai.com/v1/chat/completions", HttpStatusCode.BadRequest);

        try
        {
            await Make(new HttpClient(mockHandler)).CompleteAsync("test");
            Assert.Fail("Expected HttpRequestException");
        }
        catch (HttpRequestException) { }
    }
}

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, object> _responses = new();
    private readonly List<MockRequest> _sentRequests = new();
    private readonly Dictionary<string, Exception> _errors = new();

    public IReadOnlyList<MockRequest> SentRequests => _sentRequests;

    public void SetupResponse(string url, object response) => _responses[url] = response;

    public void SetupError(string url, HttpStatusCode statusCode)
        => _errors[url] = new HttpRequestException($"HTTP {statusCode}");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";
        _sentRequests.Add(new MockRequest
        {
            Method  = request.Method.ToString(),
            Url     = url,
            Content = await (request.Content?.ReadAsStringAsync(cancellationToken) ?? Task.FromResult(string.Empty)),
        });

        if (_errors.TryGetValue(url, out var error)) throw error;

        if (_responses.TryGetValue(url, out var response))
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

internal class MockRequest
{
    public string Method  { get; set; } = "";
    public string Url     { get; set; } = "";
    public string Content { get; set; } = "";
}
