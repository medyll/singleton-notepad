using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class MemoryTrackerServiceTests
{
    private MemoryTrackerService _service = null!;
    private MockSettingsService _settingsService = null!;
    private string _testDir = string.Empty;
    private string _memoryPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-memory-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _memoryPath = Path.Combine(_testDir, "NOTEPAD_SINGLETON_MEMORY.md");

        _settingsService = new MockSettingsService();
        _service = new MemoryTrackerService(_settingsService, _memoryPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [TestMethod]
    public async Task AppendAsync_CreatesFile_WithHeader()
    {
        var record = new ChangeRecord { Type = "Normalize", LinesChanged = 5, Preview = "test" };
        await _service.AppendAsync(record);

        Assert.IsTrue(File.Exists(_memoryPath));
        var content = await File.ReadAllTextAsync(_memoryPath);
        Assert.IsTrue(content.Contains("# Singleton Notepad"));
        Assert.IsTrue(content.Contains("Normalize"));
    }

    [TestMethod]
    public async Task AppendAsync_AppendsRecords()
    {
        var record1 = new ChangeRecord { Type = "Normalize", LinesChanged = 5, Preview = "first" };
        var record2 = new ChangeRecord { Type = "Normalize", LinesChanged = 3, Preview = "second" };

        await _service.AppendAsync(record1);
        await _service.AppendAsync(record2);

        var content = await File.ReadAllTextAsync(_memoryPath);
        Assert.IsTrue(content.Contains("first"));
        Assert.IsTrue(content.Contains("second"));
    }

    [TestMethod]
    public async Task GetHistoryAsync_ReturnsEmpty_WhenFileNotExists()
    {
        var records = await _service.GetHistoryAsync();

        Assert.AreEqual(0, records.Count);
    }

    [TestMethod]
    public async Task GetHistoryAsync_ReturnsRecords()
    {
        var record = new ChangeRecord
        {
            Type = "Normalize",
            LinesChanged = 12,
            Preview = "Restructured headings",
            BackupPath = @"C:\backups\backup_123.md",
        };
        await _service.AppendAsync(record);

        var records = await _service.GetHistoryAsync();

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("Normalize", records[0].Type);
        Assert.AreEqual(12, records[0].LinesChanged);
        Assert.IsTrue(records[0].Preview.Contains("Restructured"));
    }

    [TestMethod]
    public async Task GetHistoryAsync_RespectsMaxRecords()
    {
        for (int i = 0; i < 10; i++)
        {
            await _service.AppendAsync(new ChangeRecord { Type = "Normalize", LinesChanged = i, Preview = $"record {i}" });
        }

        var records = await _service.GetHistoryAsync(5);

        Assert.AreEqual(5, records.Count);
    }

    [TestMethod]
    public async Task AppendAsync_EscapesMarkdownPipes()
    {
        var record = new ChangeRecord { Type = "Normalize", LinesChanged = 1, Preview = "text | with | pipes" };
        await _service.AppendAsync(record);

        var content = await File.ReadAllTextAsync(_memoryPath);
        Assert.IsTrue(content.Contains("\\|"));
    }

    [TestMethod]
    public async Task AppendAsync_TruncatesLongPreview()
    {
        var longPreview = new string('x', 100);
        var record = new ChangeRecord { Type = "Normalize", LinesChanged = 1, Preview = longPreview };
        await _service.AppendAsync(record);

        var content = await File.ReadAllTextAsync(_memoryPath);
        Assert.IsTrue(content.Contains("…"));
    }
}
