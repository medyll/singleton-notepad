using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class NormalizationServiceTests
{
    private NormalizationService _service = null!;
    private MockFileService _fileService = null!;
    private MockSettingsService _settingsService = null!;
    private MockLlmProvider _llmProvider = null!;
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

        _fileService = new MockFileService();
        _settingsService = new MockSettingsService();
        _llmProvider = new MockLlmProvider();
        _service = new NormalizationService(_fileService, _settingsService, _llmProvider, _agentsPath, _backupDir);
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
    public async Task IsRateLimited_ReturnsTrue_WhenContentUnchanged()
    {
        _llmProvider.Response = "normalized";
        await _service.NormalizeAsync("same content");

        var result = _service.IsRateLimited("same content", out var remaining);

        Assert.IsTrue(result);
        Assert.IsTrue(remaining > TimeSpan.Zero);
    }

    [TestMethod]
    public void IsRateLimited_ReturnsFalse_WhenContentChanged()
    {
        _service.IsRateLimited("original content", out _);
        var result = _service.IsRateLimited("different content", out var remaining);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsFalse_ForNormalContent()
    {
        var content = "# Hello\n\nThis is normal content.";
        var result = _service.ExceedsSizeLimit(content, out var reason);

        Assert.IsFalse(result);
        Assert.AreEqual(string.Empty, reason);
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsTrue_ForLargeContent()
    {
        var content = new string('x', 600 * 1024); // 600 KB
        var result = _service.ExceedsSizeLimit(content, out var reason);

        Assert.IsTrue(result);
        Assert.IsTrue(reason.Contains("500"));
    }

    [TestMethod]
    public void ExceedsSizeLimit_ReturnsTrue_ForManyLines()
    {
        var lines = Enumerable.Range(0, 11_000).Select(i => $"Line {i}");
        var content = string.Join('\n', lines);
        var result = _service.ExceedsSizeLimit(content, out var reason);

        Assert.IsTrue(result);
        Assert.IsTrue(reason.Contains("10"));
    }

    [TestMethod]
    public async Task NormalizeAsync_ReturnsResultWithDiff()
    {
        _llmProvider.Response = "# Normalized\n\nChanged content here.";
        var original = "# Original\n\nOriginal content here.";

        var result = await _service.NormalizeAsync(original);

        Assert.AreEqual(original, result.OriginalContent);
        Assert.AreEqual("# Normalized\n\nChanged content here.", result.NormalizedContent);
        Assert.IsTrue(result.HasChanges);
        Assert.AreEqual("Ollama", result.ProviderName);
        Assert.IsTrue(result.Duration > TimeSpan.Zero);
        Assert.IsTrue(File.Exists(result.BackupPath));
    }

    [TestMethod]
    public async Task NormalizeAsync_BackupFileCreated()
    {
        _llmProvider.Response = "normalized";
        var result = await _service.NormalizeAsync("original");

        Assert.IsTrue(File.Exists(result.BackupPath));
        var backupContent = File.ReadAllText(result.BackupPath);
        Assert.AreEqual("original", backupContent);
    }

    [TestMethod]
    public async Task NormalizeAsync_CallsLlmWithPromptContainingRules()
    {
        await File.WriteAllTextAsync(_agentsPath, "Custom rule: always use H2");
        _llmProvider.Response = "done";

        await _service.NormalizeAsync("# Test");

        Assert.IsTrue(_llmProvider.LastPrompt.Contains("Custom rule: always use H2"));
        Assert.IsTrue(_llmProvider.LastPrompt.Contains("# Test"));
    }
}

internal class MockFileService : IFileService
{
    public bool FileExists { get; set; } = true;
    public string FileContent { get; set; } = string.Empty;
    public event Action? FileSaved;
    public event Action<string>? ExternalChangeDetected;

    public Task<string> LoadAsync(CancellationToken ct = default) => Task.FromResult(FileContent);
    public Task SaveAsync(string content, CancellationToken ct = default) { FileSaved?.Invoke(); return Task.CompletedTask; }
    public void Watch(Action<string> onExternalChange) { }
    public void StopWatching() { }
    public void QueueAutoSave(string content) { }
    public void CancelAutoSave() { }
}

internal class MockSettingsService : ISettingsService
{
    public Task<AppSettings> LoadAsync(CancellationToken ct = default) => Task.FromResult(new AppSettings());
    public Task SaveAsync(AppSettings settings, CancellationToken ct = default) => Task.CompletedTask;
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
