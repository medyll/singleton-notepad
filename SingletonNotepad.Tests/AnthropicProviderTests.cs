using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Tests;

[TestClass]
public class AnthropicProviderTests
{
    private const string DefaultModel = "claude-haiku-4-5-20251001";

    private static AnthropicProvider Make(HttpClient http, string apiKey = "sk-ant-test-key")
        => new(http, new StubSettingsService(new AppSettings { AnthropicApiKey = apiKey }));

    [TestMethod]
    public void Name_ReturnsAnthropic()
    {
        Assert.AreEqual("Anthropic", Make(new HttpClient()).Name);
    }

    [TestMethod]
    public async Task CompleteAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);

            Assert.AreEqual(DefaultModel, json.RootElement.GetProperty("model").GetString());
            var messages = json.RootElement.GetProperty("messages");
            Assert.AreEqual("user", messages[0].GetProperty("role").GetString());
            Assert.AreEqual("Hello", messages[0].GetProperty("content").GetString());
            Assert.AreEqual("https://api.anthropic.com/v1/messages", request.RequestUri!.AbsoluteUri);
            Assert.IsTrue(request.Headers.Contains("x-api-key"));
            Assert.IsTrue(request.Headers.Contains("anthropic-version"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[{"text":"Hi there!"}]}"""),
            };
        });

        var result = await Make(new HttpClient(handler)).CompleteAsync("system", "Hello");
        Assert.AreEqual("Hi there!", result);
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenNoContent()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"content":[]}"""),
        }));

        Assert.AreEqual(string.Empty, await Make(new HttpClient(handler)).CompleteAsync("system", "Hello"));
    }

    [TestMethod]
    public async Task CompleteAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        try
        {
            await Make(new HttpClient(handler)).CompleteAsync("system", "Hello");
            Assert.Fail("Expected HttpRequestException");
        }
        catch (HttpRequestException) { }
    }

    [TestMethod]
    public async Task CompleteAsync_SendsApiKeyInHeader()
    {
        var handler = new MockHttpHandler(request =>
        {
            Assert.IsTrue(request.Headers.TryGetValues("x-api-key", out var vals));
            Assert.AreEqual("sk-ant-test-key", vals!.First());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[{"text":"ok"}]}"""),
            });
        });

        await Make(new HttpClient(handler)).CompleteAsync("system", "Hello");
    }

    [TestMethod]
    public async Task CompleteAsync_CancellationTokenStopsRequest()
    {
        var handler = new MockHttpHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        try
        {
            await Make(new HttpClient(handler)).CompleteAsync("system", "Hello", cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException) { }
    }

    [TestMethod]
    public void GetAvailableModelsAsync_ReturnsStaticList()
    {
        var models = Make(new HttpClient()).GetAvailableModelsAsync().Result;
        Assert.IsTrue(models.Count > 0);
    }
}
