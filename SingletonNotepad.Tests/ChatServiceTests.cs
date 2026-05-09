using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class ChatServiceTests
{
    private static ChatService MakeService(ILlmProvider provider, string llmProviderName = "TestProvider")
    {
        var settings = new StubSettingsService(new AppSettings { LlmProvider = llmProviderName });
        var selector = new LlmProviderSelector([provider]);
        var skillService = new SkillService();
        return new ChatService(selector, settings, skillService);
    }

    [TestMethod]
    public async Task SendAsync_CallsProviderWithSystemPrompt()
    {
        var mockProvider = new MockChatProvider { Response = "Hello back!" };
        var service = MakeService(mockProvider);

        var (result, _) = await service.SendAsync("Hello", null);

        Assert.AreEqual("Hello back!", result);
        Assert.IsTrue(mockProvider.LastSystemPrompt.Contains("assistant"));
        Assert.AreEqual("Hello", mockProvider.LastUserMessage);
    }

    [TestMethod]
    public async Task SendAsync_IncludesContextInSystemPrompt()
    {
        var mockProvider = new MockChatProvider { Response = "Done" };
        var service = MakeService(mockProvider);

        await service.SendAsync("Fix this", "Some note content here");

        Assert.IsTrue(mockProvider.LastSystemPrompt.Contains("Some note content here"));
    }

    [TestMethod]
    public async Task SendAsync_UsesShortPromptWhenNoContext()
    {
        var mockProvider = new MockChatProvider { Response = "Done" };
        var service = MakeService(mockProvider);

        await service.SendAsync("Help me", null);

        Assert.IsFalse(mockProvider.LastSystemPrompt.Contains("<contenu_note>"));
    }
}

internal class MockChatProvider : ILlmProvider
{
    public string Name => "TestProvider";
    public string Response { get; set; } = "mock response";
    public string LastSystemPrompt { get; set; } = string.Empty;
    public string LastUserMessage { get; set; } = string.Empty;

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        LastSystemPrompt = systemPrompt;
        LastUserMessage = userMessage;
        return Task.FromResult(Response);
    }

    public Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(["test-model"]);
    }
}
