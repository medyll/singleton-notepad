using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class NormalizationServiceTests
{
    private NormalizationService _service = null!;
    private MockSettingsService _settingsService = null!;
    private MockLlmProvider _llmProvider = null!;
    private MockLlmProviderSelector _providerSelector = null!;
    private string _testDir = string.Empty;
    private string _agentsPath = string.Empty;
    private string _backupDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-norm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _agentsPath = Path.Combine(_testDir, "NOTEPAD_SINGLETON_AGENTS.md");
        _backupDir = Path.Combine(_testDir, "backups");

        _settingsService = new MockSettingsService();
        _llmProvider = new MockLlmProvider();
        _providerSelector = new MockLlmProviderSelector(_llmProvider);
        _service = new NormalizationService(null!, _settingsService, _providerSelector, _agentsPath, _backupDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [TestMethod]
    public async Task LoadRulesAsync_ReturnsEmpty_WhenFileNotExists()
    {
        var rules = await _service.LoadRulesAsync();

        Assert.AreEqual(string.Empty, rules.Content);
    }

    [TestMethod]
    public async Task LoadRulesAsync_ReturnsContent_WhenFileExists()
    {
        await File.WriteAllTextAsync(_agentsPath, "# Rules\n\n- Rule 1\n- Rule 2");
        var rules = await _service.LoadRulesAsync();

        Assert.AreEqual("# Rules\n\n- Rule 1\n- Rule 2", rules.Content);
    }

    [TestMethod]
    public void IsRateLimited_ReturnsFalse_OnFirstCall()
    {
        var result = _service.IsRateLimited("test content", out var remaining);

        Assert.IsFalse(result);
        Assert.AreEqual(TimeSpan.Zero, remaining);
    }

    [TestMethod]
    public void IsRateLimited_ReturnsFalse_WhenContentChanged()
    {
        _service.IsRateLimited("content v1", out _);

        var result = _service.IsRateLimited("content v2", out var remaining);

        Assert.IsFalse(result);
    }

[TestMethod]
    public void IsRateLimited_ReturnsTrue_WhenContentUnchanged()
    {
        var content = "same content";
        var rule = new NormalizationRule { Content = "" };
        _ = _service.NormalizeAsync(content).Result;
        Thread.Sleep(50);
        var result = _service.IsRateLimited(content, out var remaining);

        Assert.IsTrue(result, $"Expected rate limited, remaining={remaining}");
        Assert.IsTrue(remaining >= TimeSpan.Zero, $"remaining={remaining}");
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsTrue_ForManyLines()
    {
        var manyLines = string.Join("\n", Enumerable.Range(0, 15000).Select(i => $"Line {i}"));

        var result = _service.ExceedsSizeLimit(manyLines, out var reason);

        Assert.IsTrue(result);
        var normalized = reason.Replace("\u00A0", "").Replace("\u202F", "").Replace(",", "").Replace(" ", "");
        Assert.IsTrue(normalized.Contains("10000"), $"reason={reason}, normalized={normalized}");
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsFalse_ForNormalContent()
    {
        var result = _service.ExceedsSizeLimit("normal content", out var reason);

        Assert.IsFalse(result);
        Assert.AreEqual(string.Empty, reason);
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsTrue_ForLargeContent()
    {
        var largeContent = new string('x', 600 * 1024);

        var result = _service.ExceedsSizeLimit(largeContent, out var reason);

        Assert.IsTrue(result);
        Assert.IsTrue(reason.Contains("500 KB"));
    }

    [TestMethod]
    public async Task NormalizeAsync_ReturnsResultWithDiff()
    {
        var result = await _service.NormalizeAsync("# Hello\n\nWorld");

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Diff);
        Assert.AreEqual("# Hello\n\nWorld", result.OriginalContent);
        Assert.IsTrue(result.LinesAdded >= 0);
    }

    [TestMethod]
    public async Task NormalizeAsync_CallsLlmWithPromptContainingRules()
    {
        await File.WriteAllTextAsync(_agentsPath, "Use active voice");

        await _service.NormalizeAsync("test content");

        Assert.IsTrue(_llmProvider.LastPrompt.Contains("active voice"));
    }

    [TestMethod]
    public async Task NormalizeAsync_BackupFileCreated()
    {
        var result = await _service.NormalizeAsync("content to backup");

        Assert.IsFalse(string.IsNullOrEmpty(result.BackupPath));
        Assert.IsTrue(File.Exists(result.BackupPath));
    }
}

internal class MockSettingsService : ISettingsService
{
    public Task<AppSettings> LoadAsync(CancellationToken ct = default) => Task.FromResult(new AppSettings());
    public Task SaveAsync(AppSettings settings, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> ProtectApiKeyAsync(string plainText, CancellationToken ct = default) => Task.FromResult(plainText ?? string.Empty);
    public Task<string> UnprotectApiKeyAsync(string protectedText, CancellationToken ct = default) => Task.FromResult(protectedText ?? string.Empty);
}

internal class MockLlmProvider : ILlmProvider
{
    public string Name => "Ollama";
    public string Response { get; set; } = "mock response";
    public string LastPrompt { get; set; } = string.Empty;

    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        LastPrompt = prompt;
        return Task.FromResult(Response);
    }
}

internal class MockLlmProviderSelector : ILlmProviderSelector
{
    private readonly ILlmProvider _provider;

    public MockLlmProviderSelector(ILlmProvider provider)
    {
        _provider = provider;
    }

    public ILlmProvider Current => _provider;
    public string CurrentName => _provider.Name;
    public IReadOnlyList<string> AvailableProviders => new[] { _provider.Name };

    public void SelectProvider(string providerName)
    {
    }
}