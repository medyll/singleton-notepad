using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Tests;

[TestClass]
public class OpenAiProviderTests
{
    [TestMethod]
    public void Name_ReturnsOpenAi()
    {
        var httpClient = new HttpClient();
        var provider = new OpenAiProvider(httpClient, "sk-test-key");
        Assert.AreEqual("OpenAI", provider.Name);
    }

    [TestMethod]
    public async Task CompleteAsync_SendsCorrectRequest()
    {
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);

        mockHandler.SetupResponse(
            "https://api.openai.com/v1/chat/completions",
            new OpenAiChatResponse
            {
                Choices = new[]
                {
                    new OpenAiChoice
                    {
                        Message = new OpenAiMessage { Role = "assistant", Content = "Normalized markdown content" }
                    }
                }
            });

        var provider = new OpenAiProvider(httpClient, "sk-test-key", "https://api.openai.com/v1", "gpt-4o-mini");
        var result = await provider.CompleteAsync("Fix my markdown");

        Assert.AreEqual("Normalized markdown content", result);
        var sentRequest = mockHandler.SentRequests.First();
        Assert.IsTrue(sentRequest.Content.Contains("gpt-4o-mini"));
        Assert.IsTrue(sentRequest.Content.Contains("Fix my markdown"));
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenNoChoices()
    {
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);

        mockHandler.SetupResponse(
            "https://api.openai.com/v1/chat/completions",
            new OpenAiChatResponse { Choices = Array.Empty<OpenAiChoice>() });

        var provider = new OpenAiProvider(httpClient, "sk-test-key");
        var result = await provider.CompleteAsync("test");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task CompleteAsync_Throws_OnHttpError()
    {
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);

        mockHandler.SetupError("https://api.openai.com/v1/chat/completions", System.Net.HttpStatusCode.BadRequest);

        var provider = new OpenAiProvider(httpClient, "sk-test-key");

        try
        {
            await provider.CompleteAsync("test");
            Assert.Fail("Expected HttpRequestException");
        }
        catch (System.Net.Http.HttpRequestException)
        {
        }
    }
}

internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, object> _responses = new();
    private readonly List<MockRequest> _sentRequests = new();
    private readonly Dictionary<string, Exception> _errors = new();

    public IReadOnlyList<MockRequest> SentRequests => _sentRequests;

    public void SetupResponse(string url, object response)
    {
        _responses[url] = response;
    }

    public void SetupError(string url, System.Net.HttpStatusCode statusCode)
    {
        _errors[url] = new System.Net.Http.HttpRequestException($"HTTP {statusCode}");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";

        _sentRequests.Add(new MockRequest
        {
            Method = request.Method.ToString(),
            Url = url,
            Content = await request.Content?.ReadAsStringAsync(cancellationToken) ?? ""
        });

        if (_errors.TryGetValue(url, out var error))
            throw error;

        if (_responses.TryGetValue(url, out var response))
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        }

        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
    }
}

internal class MockRequest
{
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public string Content { get; set; } = "";
}