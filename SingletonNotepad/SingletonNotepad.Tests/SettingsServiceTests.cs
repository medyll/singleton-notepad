using Microsoft.VisualStudio.TestTools.UnitTesting;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class SettingsServiceTests
{
    private ISettingsService _settingsService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _settingsService = new SettingsService();
    }

    [TestMethod]
    public void Get_ReturnsDefaultValue_WhenKeyNotExists()
    {
        // Arrange
        const string key = "TestKey_NotExists";
        const string expected = "DefaultValue";

        // Act
        var actual = _settingsService.Get(key, expected);

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Set_And_Get_RoundTrip()
    {
        // Arrange
        const string key = "TestKey_RoundTrip";
        const string value = "TestValue";

        // Act
        _settingsService.Set(key, value);
        var actual = _settingsService.Get(key, string.Empty);

        // Assert
        Assert.AreEqual(value, actual);
    }

    [TestMethod]
    public void Get_IntValue_CorrectType()
    {
        // Arrange
        const string key = "TestKey_Int";
        const int value = 42;

        // Act
        _settingsService.Set(key, value);
        var actual = _settingsService.Get(key, 0);

        // Assert
        Assert.AreEqual(value, actual);
    }
}
