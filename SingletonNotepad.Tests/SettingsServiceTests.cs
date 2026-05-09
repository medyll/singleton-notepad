using System.Text.Json;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class SettingsServiceTests
{
    private string _testDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private SettingsService CreateService() => new(_testDir);

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var service = CreateService();
        var settings = await service.LoadAsync();

        Assert.IsNotNull(settings);
        Assert.AreEqual("System", settings.Theme);
        Assert.IsTrue(settings.AutoSave);
        Assert.AreEqual(2000, settings.AutoSaveDelayMs);
        Assert.AreEqual(string.Empty, settings.NotesFilePath);
    }

    [TestMethod]
    public async Task SaveAsync_And_LoadAsync_RoundTrip()
    {
        var service = CreateService();
        var expected = new AppSettings
        {
            Theme = "Dark",
            NotesFilePath = @"C:\test\notes.md",
            AutoSave = false,
            AutoSaveDelayMs = 5000,
        };

        await service.SaveAsync(expected);
        var loaded = await service.LoadAsync();

        Assert.AreEqual("Dark", loaded.Theme);
        Assert.AreEqual(@"C:\test\notes.md", loaded.NotesFilePath);
        Assert.IsFalse(loaded.AutoSave);
        Assert.AreEqual(5000, loaded.AutoSaveDelayMs);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesDirectory_WhenNotExists()
    {
        var nestedDir = Path.Combine(_testDir, "nested", "deep");
        var service = new SettingsService(nestedDir);

        await service.SaveAsync(new AppSettings { Theme = "Light" });

        var loaded = await service.LoadAsync();
        Assert.AreEqual("Light", loaded.Theme);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaults_WhenJsonIsCorrupt()
    {
        var settingsPath = Path.Combine(_testDir, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{ invalid json }}}");

        var service = CreateService();
        var settings = await service.LoadAsync();

        Assert.IsNotNull(settings);
        Assert.AreEqual("System", settings.Theme);
    }

    [TestMethod]
    public async Task LoadAsync_CachesResult()
    {
        var service = CreateService();
        var first = await service.LoadAsync();
        first.Theme = "Dark";
        await service.SaveAsync(first);

        var second = await service.LoadAsync();
        Assert.AreSame(first, second);
    }

    [TestMethod]
    public async Task ProtectApiKeyAsync_And_UnprotectApiKeyAsync_RoundTrip()
    {
        var service = CreateService();
        var key = "sk-test-secret-key-12345";

        var protectedKey = await service.ProtectApiKeyAsync(key);
        Assert.AreNotEqual(key, protectedKey);
        Assert.IsFalse(string.IsNullOrEmpty(protectedKey));

        var unprotectedKey = await service.UnprotectApiKeyAsync(protectedKey);
        Assert.AreEqual(key, unprotectedKey);
    }

    [TestMethod]
    public async Task ProtectApiKeyAsync_ReturnsEmpty_WhenInputEmpty()
    {
        var service = CreateService();
        var result = await service.ProtectApiKeyAsync("");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task UnprotectApiKeyAsync_ReturnsEmpty_WhenInputEmpty()
    {
        var service = CreateService();
        var result = await service.UnprotectApiKeyAsync("");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task UnprotectApiKeyAsync_ReturnsEmpty_WhenInvalidBase64()
    {
        var service = CreateService();
        var result = await service.UnprotectApiKeyAsync("not-valid-base64!!!");
        Assert.AreEqual(string.Empty, result);
    }
}
