using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class FileServiceTests
{
    private string _testDir = string.Empty;
    private string _testFilePath = string.Empty;
    private string _settingsDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-fs-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testFilePath = Path.Combine(_testDir, "test.md");

        _settingsDir = Path.Combine(Path.GetTempPath(), $"sn-settings-{Guid.NewGuid()}");
        Directory.CreateDirectory(_settingsDir);

        var settings = new AppSettings { NotesFilePath = _testFilePath };
        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_settingsDir, "settings.json"), json);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        if (Directory.Exists(_settingsDir))
            Directory.Delete(_settingsDir, true);
    }

    private FileService CreateService()
    {
        var settingsService = new SettingsService(_settingsDir);
        return new FileService(settingsService);
    }

    [TestMethod]
    public async Task LoadAsync_CreatesFile_WhenNotExists()
    {
        using var service = CreateService();
        var content = await service.LoadAsync();

        Assert.AreEqual(string.Empty, content);
        Assert.IsTrue(File.Exists(_testFilePath));
    }

    [TestMethod]
    public async Task SaveAsync_WritesContent()
    {
        using var service = CreateService();
        await service.LoadAsync();
        await service.SaveAsync("# Test\n\nContent");

        var saved = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual("# Test\n\nContent", saved);
    }

    [TestMethod]
    public async Task LoadAsync_ReadsExistingFile()
    {
        await File.WriteAllTextAsync(_testFilePath, "# Existing\n\nContent");

        using var service = CreateService();
        var content = await service.LoadAsync();
        Assert.AreEqual("# Existing\n\nContent", content);
    }

    [TestMethod]
    public async Task SaveAsync_UsesRetryLogic_WhenFileLocked()
    {
        using var service = CreateService();
        await service.LoadAsync();
        await service.SaveAsync("Initial");

        var saved = false;
        using (var fs = new FileStream(_testFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var saveTask = service.SaveAsync("# Test");
            await Task.Delay(100);
            fs.Close();
            await saveTask;
            saved = true;
        }

        Assert.IsTrue(saved);
        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual("# Test", content);
    }

    [TestMethod]
    public async Task QueueAutoSave_DebouncesWrites()
    {
        using var service = CreateService();
        await service.LoadAsync();

        var tcs = new TaskCompletionSource<bool>();
        service.FileSaved += () => tcs.TrySetResult(true);

        service.QueueAutoSave("First");
        service.QueueAutoSave("Second");
        service.QueueAutoSave("Third");

        var signaled = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(signaled);

        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual("Third", content);
    }

    [TestMethod]
    public async Task CancelAutoSave_PreventsPendingWrite()
    {
        using var service = CreateService();
        await service.LoadAsync();

        service.QueueAutoSave("Should not save");
        service.CancelAutoSave();

        await Task.Delay(3000);

        var content = await File.ReadAllTextAsync(_testFilePath);
        Assert.AreEqual(string.Empty, content);
    }

    [TestMethod]
    public void Dispose_StopsWatcherAndTimers()
    {
        var service = CreateService();
        service.Dispose();

        // Should not throw — double dispose is safe
        service.Dispose();
    }
}
