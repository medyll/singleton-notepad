using Microsoft.VisualStudio.TestTools.UnitTesting;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class AppSettingsTests
{
    [TestMethod]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.AreEqual(2000, settings.AutoSaveDebounceMs);
        Assert.AreEqual("System", settings.Theme);
        Assert.AreEqual("Ollama", settings.ActiveLlmProvider);
        Assert.AreEqual(1200, settings.WindowWidth);
        Assert.AreEqual(800, settings.WindowHeight);
        Assert.AreEqual(5, settings.MinAutoNormalizeIntervalMinutes);
        Assert.IsFalse(settings.AutoNormalizeOnClose);
    }

    [TestMethod]
    public void AppSettings_SetAndGetProperties_Work()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.SingletonFilePath = @"C:\Test\notes.md";
        settings.RulesFilePath = @"C:\Test\rules.md";
        settings.Theme = "Dark";
        settings.AutoSaveDebounceMs = 1000;
        settings.WindowWidth = 1920;
        settings.WindowHeight = 1080;

        // Assert
        Assert.AreEqual(@"C:\Test\notes.md", settings.SingletonFilePath);
        Assert.AreEqual(@"C:\Test\rules.md", settings.RulesFilePath);
        Assert.AreEqual("Dark", settings.Theme);
        Assert.AreEqual(1000, settings.AutoSaveDebounceMs);
        Assert.AreEqual(1920, settings.WindowWidth);
        Assert.AreEqual(1080, settings.WindowHeight);
    }
}

[TestClass]
public class SettingsServiceIntegrationTests
{
    private ISettingsService _settingsService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _settingsService = new SettingsService();
        // Clear any existing settings
        _settingsService.ResetToDefaults();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _settingsService.ResetToDefaults();
    }

    [TestMethod]
    public void GetSettings_ReturnsDefaults_WhenNoSettingsExist()
    {
        // Arrange - settings were reset

        // Act
        var settings = _settingsService.GetSettings();

        // Assert
        Assert.IsNotNull(settings);
        Assert.AreEqual(2000, settings.AutoSaveDebounceMs);
        Assert.AreEqual("System", settings.Theme);
        Assert.IsFalse(string.IsNullOrEmpty(settings.SingletonFilePath));
        Assert.IsFalse(string.IsNullOrEmpty(settings.RulesFilePath));
    }

    [TestMethod]
    public void SaveSettings_And_GetSettings_RoundTrip()
    {
        // Arrange
        var original = new AppSettings
        {
            SingletonFilePath = @"C:\Test\my-notes.md",
            RulesFilePath = @"C:\Test\my-rules.md",
            Theme = "Dark",
            AutoSaveDebounceMs = 3000,
            WindowWidth = 1600,
            WindowHeight = 900
        };

        // Act
        _settingsService.SaveSettings(original);
        var retrieved = _settingsService.GetSettings();

        // Assert
        Assert.AreEqual(original.SingletonFilePath, retrieved.SingletonFilePath);
        Assert.AreEqual(original.RulesFilePath, retrieved.RulesFilePath);
        Assert.AreEqual(original.Theme, retrieved.Theme);
        Assert.AreEqual(original.AutoSaveDebounceMs, retrieved.AutoSaveDebounceMs);
        Assert.AreEqual(original.WindowWidth, retrieved.WindowWidth);
        Assert.AreEqual(original.WindowHeight, retrieved.WindowHeight);
    }

    [TestMethod]
    public void ResetToDefaults_ClearsAllSettings()
    {
        // Arrange
        var custom = new AppSettings { Theme = "Dark", AutoSaveDebounceMs = 5000 };
        _settingsService.SaveSettings(custom);

        // Act
        _settingsService.ResetToDefaults();
        var settings = _settingsService.GetSettings();

        // Assert
        Assert.AreEqual("System", settings.Theme);
        Assert.AreEqual(2000, settings.AutoSaveDebounceMs);
    }

    [TestMethod]
    public void SaveSettings_PreservesWindowGeometry()
    {
        // Arrange
        var settings = new AppSettings
        {
            WindowLeft = 100,
            WindowTop = 200,
            WindowWidth = 1400,
            WindowHeight = 1000
        };

        // Act
        _settingsService.SaveSettings(settings);
        var retrieved = _settingsService.GetSettings();

        // Assert
        Assert.AreEqual(100, retrieved.WindowLeft);
        Assert.AreEqual(200, retrieved.WindowTop);
        Assert.AreEqual(1400, retrieved.WindowWidth);
        Assert.AreEqual(1000, retrieved.WindowHeight);
    }
}
