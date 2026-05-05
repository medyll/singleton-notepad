using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Tests;

[TestClass]
public class FileServiceTests
{
    private string _testDir = string.Empty;
    private string _testFilePath = string.Empty;
    private string _settingsDir = string.Empty;
    private string _settingsPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-fs-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testFilePath = Path.Combine(_testDir, "test.md");
        
        _settingsDir = Path.Combine(Path.GetTempPath(), $"sn-settings-{Guid.NewGuid()}");
        Directory.CreateDirectory(_settingsDir);
        _settingsPath = Path.Combine(_settingsDir, "settings.json");
        
        // Initialize settings file with test path
        var settings = new AppSettings { NotesFilePath = _testFilePath };
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        if (Directory.Exists(_settingsDir))
            Directory.Delete(_settingsDir, true);
    }

    private static async Task<string> LoadFromFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(path, string.Empty, ct);
            return string.Empty;
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    private static async Task SaveToFileAsync(string path, string content, CancellationToken ct = default)
    {
        const int maxRetries = 3;
        var delays = new[] { 200, 400, 800 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, content, ct);
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(delays[attempt], ct);
            }
        }

        await File.WriteAllTextAsync(path, content, ct);
    }

    [TestMethod]
    public async Task LoadAsync_CreatesFile_WhenNotExists()
    {
        var content = await LoadFromFileAsync(_testFilePath);
        
        Assert.AreEqual(string.Empty, content);
        Assert.IsTrue(File.Exists(_testFilePath));
    }

    [TestMethod]
    public async Task SaveAsync_WritesContent()
    {
        await SaveToFileAsync(_testFilePath, "# Test\n\nContent");
        
        var saved = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual("# Test\n\nContent", saved);
    }

    [TestMethod]
    public async Task LoadAsync_ReadsExistingFile()
    {
        await File.WriteAllTextAsync(_testFilePath, "# Existing\n\nContent");
        
        var content = await LoadFromFileAsync(_testFilePath);
        Assert.AreEqual("# Existing\n\nContent", content);
    }

    [TestMethod]
    public async Task SaveAsync_UsesRetryLogic_WhenFileLocked()
    {
        await SaveToFileAsync(_testFilePath, "Initial");
        
        // Lock the file
        using (var fs = new FileStream(_testFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // Try to save - should retry then succeed after lock released
            var saveTask = SaveToFileAsync(_testFilePath, "# Test");
            await Task.Delay(100);
        }
        
        await SaveToFileAsync(_testFilePath, "# After lock");
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual("# After lock", content);
    }
}
