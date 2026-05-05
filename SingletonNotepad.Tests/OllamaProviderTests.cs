using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Tests;

[TestClass]
public class OllamaProviderTests
{
    [TestMethod]
    public void Name_ReturnsOllama()
    {
        var httpClient = new HttpClient();
        var provider = new OllamaProvider(httpClient);

        Assert.AreEqual("Ollama", provider.Name);
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

        var httpClient = new HttpClient(handler);
        var provider = new OllamaProvider(httpClient);

        var result = await provider.CompleteAsync("Hello");

        Assert.AreEqual("Hi there!", result);
    }

    [TestMethod]
    public async Task CompleteAsync_ReturnsEmpty_WhenResponseIsEmpty()
    {
        var handler = new MockHttpHandler(_ =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"response":""}"""),
            });
        });

        var httpClient = new HttpClient(handler);
        var provider = new OllamaProvider(httpClient);

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
        var provider = new OllamaProvider(httpClient);

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
    public async Task CompleteAsync_ThrowsOnTimeout()
    {
        var handler = new MockHttpHandler(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(100),
        };
        var provider = new OllamaProvider(httpClient);

        try
        {
            await provider.CompleteAsync("Hello");
            Assert.Fail("Expected TaskCanceledException");
        }
        catch (TaskCanceledException)
        {
        }
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

        var httpClient = new HttpClient(handler);
        var provider = new OllamaProvider(httpClient, "http://192.168.1.42:11434", "mistral");

        await provider.CompleteAsync("Hello");
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

        var httpClient = new HttpClient(handler);
        var provider = new OllamaProvider(httpClient, "http://localhost:11434", "mistral");

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
        var provider = new OllamaProvider(httpClient);

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

internal class MockHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _handler(request);
    }
}
