using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class OllamaProviderTests
{
    private static OllamaProvider Make(HttpClient http, string endpoint = "http://localhost:11434", string model = "llama3")
        => new(http, new StubSettingsService(new AppSettings { OllamaEndpoint = endpoint, OllamaModel = model }));

    [TestMethod]
    public void Name_ReturnsOllama()
    {
        Assert.AreEqual("Ollama", Make(new HttpClient()).Name);
    }

    [TestMethod]
    public async Task CompleteAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);

            Assert.AreEqual("llama3", json.RootElement.GetProperty("model").GetString());
            Assert.AreEqual("Hello", json.RootElement.GetProperty("prompt").GetString());
            Assert.IsFalse(json.RootElement.GetProperty("stream").GetBoolean());
            Assert.AreEqual("http://localhost:11434/api/generate", request.RequestUri!.AbsoluteUri);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response":"Hi there!"}"""),
            };
        });

        var result = await Make(new HttpClient(handler)).CompleteAsync("Hello");
        Assert.AreEqual("Hi there!", result);
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenResponseIsEmpty()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"response":""}"""),
        }));

        Assert.AreEqual(string.Empty, await Make(new HttpClient(handler)).CompleteAsync("Hello"));
    }

    [TestMethod]
    public async Task CompleteAsync_ThrowsOnHttpError()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        try
        {
            await Make(new HttpClient(handler)).CompleteAsync("Hello");
            Assert.Fail("Expected HttpRequestException");
        }
        catch (HttpRequestException) { }
    }

    [TestMethod]
    public async Task CompleteAsync_ThrowsOnTimeout()
    {
        var handler = new MockHttpHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        try
        {
            await Make(http).CompleteAsync("Hello");
            Assert.Fail("Expected TaskCanceledException");
        }
        catch (TaskCanceledException) { }
    }

    [TestMethod]
    public async Task CompleteAsync_UsesCustomEndpoint()
    {
        var handler = new MockHttpHandler(request =>
        {
            Assert.AreEqual("http://192.168.1.42:11434/api/generate", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response":"ok"}"""),
            });
        });

        await Make(new HttpClient(handler), "http://192.168.1.42:11434", "mistral").CompleteAsync("Hello");
    }

    [TestMethod]
    public async Task CompleteAsync_UsesCustomModel()
    {
        var handler = new MockHttpHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.AreEqual("mistral", json.RootElement.GetProperty("model").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response":"ok"}"""),
            };
        });

        await Make(new HttpClient(handler), model: "mistral").CompleteAsync("Hello");
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
            await Make(new HttpClient(handler)).CompleteAsync("Hello", cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException) { }
    }
}

// Shared across provider test files
internal class StubSettingsService : ISettingsService
{
    private readonly AppSettings _settings;
    public StubSettingsService(AppSettings? settings = null) => _settings = settings ?? new AppSettings();
    public Task<AppSettings> LoadAsync(CancellationToken ct = default) => Task.FromResult(_settings);
    public Task SaveAsync(AppSettings settings, CancellationToken ct = default) => Task.CompletedTask;
}

internal class MockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
    public MockHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await _handler(request);
}
