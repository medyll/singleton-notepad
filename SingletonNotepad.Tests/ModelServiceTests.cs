using System.Net;
using System.Text.Json;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;
using SingletonNotepad.Tests;

namespace SingletonNotepad.Tests;

[TestClass]
public class ModelServiceTests
{
    private static ModelService MakeService(IEnumerable<ILlmProvider> providers)
        => new(providers);

    [TestMethod]
    public async Task GetModelsForProviderAsync_ReturnsModelsFromOllama()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"models":[{"name":"llama3:latest"},{"name":"qwen3.5:latest"}]}"""),
        }));

        var settings = new StubSettingsService(new AppSettings { OllamaEndpoint = "http://localhost:11434" });
        var ollama = new OllamaProvider(new HttpClient(handler), settings);
        var service = MakeService([ollama]);

        var models = await service.GetModelsForProviderAsync("Ollama");

        Assert.AreEqual(2, models.Count);
        Assert.IsTrue(models.Contains("llama3:latest"));
        Assert.IsTrue(models.Contains("qwen3.5:latest"));
    }

    [TestMethod]
    public async Task GetModelsForProviderAsync_ReturnsStaticModelsForAnthropic()
    {
        var settings = new StubSettingsService();
        var anthropic = new AnthropicProvider(new HttpClient(), settings);
        var service = MakeService([anthropic]);

        var models = await service.GetModelsForProviderAsync("Anthropic");

        Assert.IsTrue(models.Count > 0);
        Assert.IsTrue(models.Any(m => m.StartsWith("claude-")));
    }

    [TestMethod]
    public async Task GetModelsForProviderAsync_ReturnsEmptyForUnknownProvider()
    {
        var service = MakeService([]);
        var models = await service.GetModelsForProviderAsync("UnknownProvider");
        Assert.AreEqual(0, models.Count);
    }

    [TestMethod]
    public async Task GetModelsForProviderAsync_ReturnsEmptyOnHttpError()
    {
        var handler = new MockHttpHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var settings = new StubSettingsService(new AppSettings { OllamaEndpoint = "http://localhost:11434" });
        var ollama = new OllamaProvider(new HttpClient(handler), settings);
        var service = MakeService([ollama]);

        var models = await service.GetModelsForProviderAsync("Ollama");

        Assert.AreEqual(0, models.Count);
    }
}
