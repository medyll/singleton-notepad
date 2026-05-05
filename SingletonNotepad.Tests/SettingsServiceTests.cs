using System.Text.Json;
using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Tests;

[TestClass]
public class SettingsServiceTests
{
    private string _testDir = string.Empty;
    private string _settingsPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sn-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _settingsPath = Path.Combine(_testDir, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private static async Task<AppSettings> LoadFromFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true }) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    private static async Task SaveToFileAsync(string path, AppSettings settings, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true });
        await File.WriteAllTextAsync(path, json, ct);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var settings = await LoadFromFileAsync(_settingsPath);

        Assert.IsNotNull(settings);
        Assert.AreEqual("System", settings.Theme);
        Assert.IsTrue(settings.AutoSave);
        Assert.AreEqual(2000, settings.AutoSaveDelayMs);
        Assert.AreEqual(string.Empty, settings.NotesFilePath);
    }

    [TestMethod]
    public async Task SaveAsync_And_LoadAsync_RoundTrip()
    {
        var expected = new AppSettings
        {
            Theme = "Dark",
            NotesFilePath = @"C:\test\notes.md",
            AutoSave = false,
            AutoSaveDelayMs = 5000,
        };

        await SaveToFileAsync(_settingsPath, expected);
        var loaded = await LoadFromFileAsync(_settingsPath);

        Assert.AreEqual("Dark", loaded.Theme);
        Assert.AreEqual(@"C:\test\notes.md", loaded.NotesFilePath);
        Assert.IsFalse(loaded.AutoSave);
        Assert.AreEqual(5000, loaded.AutoSaveDelayMs);
    }

    [TestMethod]
    public async Task SaveAsync_CreatesDirectory_WhenNotExists()
    {
        var nestedDir = Path.Combine(_testDir, "nested", "deep");
        var nestedPath = Path.Combine(nestedDir, "settings.json");

        await SaveToFileAsync(nestedPath, new AppSettings { Theme = "Light" });

        Assert.IsTrue(File.Exists(nestedPath));

        if (Directory.Exists(nestedDir))
            Directory.Delete(nestedDir, true);
    }

    [TestMethod]
    public async Task LoadAsync_ReturnsDefaults_WhenJsonIsCorrupt()
    {
        await File.WriteAllTextAsync(_settingsPath, "{ invalid json }}}");

        var settings = await LoadFromFileAsync(_settingsPath);

        Assert.IsNotNull(settings);
        Assert.AreEqual("System", settings.Theme);
    }
}
