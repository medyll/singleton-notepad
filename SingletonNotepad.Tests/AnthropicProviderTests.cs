using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Tests;

[TestClass]
public class AnthropicProviderTests
{
    [TestMethod]
    public void Name_ReturnsAnthropic()
    {
        var httpClient = new HttpClient();
        var provider = new AnthropicProvider(httpClient);

        Assert.AreEqual("Anthropic", provider.Name);
    }

    [TestMethod]
    public async Task CompleteAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);

            Assert.AreEqual("claude-3-5-haiku-20240620", json.RootElement.GetProperty("model").GetString());
            var messages = json.RootElement.GetProperty("messages");
            Assert.AreEqual("user", messages[0].GetProperty("role").GetString());
            Assert.AreEqual("Hello", messages[0].GetProperty("content").GetString());
            Assert.AreEqual("https://api.anthropic.com/v1/messages", request.RequestUri!.AbsoluteUri);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[{"text":"Hi there!"}]}"""),
            };
        });

        var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider(httpClient);

        var result = await provider.CompleteAsync("Hello");

        Assert.AreEqual("Hi there!", result);
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenNoContent()
    {
        var handler = new MockHttpHandler(_ =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[]}"""),
            });
        });

        var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider(httpClient);

        var result = await provider.CompleteAsync("Hello");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task CompleteAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpHandler(_ =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider(httpClient);

        try
        {
            await provider.CompleteAsync("Hello");
            Assert.Fail("Expected HttpRequestException");
        }
        catch (HttpRequestException)
        {
        }
    }

    [TestMethod]
    public async Task CompleteAsync_UsesCustomModel()
    {
        var handler = new MockHttpHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.AreEqual("claude-3-5-sonnet-20240701", json.RootElement.GetProperty("model").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[{"text":"ok"}]}"""),
            };
        });

        var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider(httpClient, "claude-3-5-sonnet-20240701");

        await provider.CompleteAsync("Hello");
    }

    [TestMethod]
    public async Task CompleteAsync_CancellationTokenStopsRequest()
    {
        var handler = new MockHttpHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider(httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        try
        {
            await provider.CompleteAsync("Hello", cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
        }
    }
}

internal class AnthropicMockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public AnthropicMockHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _handler(request);
    }
}