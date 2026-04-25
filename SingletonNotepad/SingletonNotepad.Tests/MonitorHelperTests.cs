using Microsoft.VisualStudio.TestTools.UnitTesting;
using SingletonNotepad.Core.Helpers;

namespace SingletonNotepad.Tests;

[TestClass]
public class MonitorHelperTests
{
    [TestMethod]
    public void IsOnPrimaryMonitor_ValidCoordinates_ReturnsTrue()
    {
        // Arrange - use coordinates that should be on primary monitor
        // (0, 0) is always on the primary monitor
        double x = 100;
        double y = 100;

        // Act
        var result = MonitorHelper.IsOnPrimaryMonitor(x, y);

        // Assert
        Assert.IsTrue(result, "Point (100, 100) should be on primary monitor");
    }

    [TestMethod]
    public void IsOnPrimaryMonitor_NegativeCoordinates_ReturnsFalse()
    {
        // Arrange - negative coordinates are off-screen
        double x = -1000;
        double y = -1000;

        // Act
        var result = MonitorHelper.IsOnPrimaryMonitor(x, y);

        // Assert
        Assert.IsFalse(result, "Negative coordinates should be off-screen");
    }

    [TestMethod]
    public void IsOnPrimaryMonitor_FarCoordinates_ReturnsFalse()
    {
        // Arrange - coordinates far beyond any reasonable screen
        double x = 100000;
        double y = 100000;

        // Act
        var result = MonitorHelper.IsOnPrimaryMonitor(x, y);

        // Assert
        Assert.IsFalse(result, "Very large coordinates should be off-screen");
    }

    [TestMethod]
    public void GetPrimaryMonitorWorkArea_ReturnsValidRectangle()
    {
        // Act
        var (left, top, width, height) = MonitorHelper.GetPrimaryMonitorWorkArea();

        // Assert
        Assert.IsTrue(width > 0, "Screen width should be positive");
        Assert.IsTrue(height > 0, "Screen height should be positive");
        Assert.IsTrue(width >= 800, "Screen width should be at least 800");
        Assert.IsTrue(height >= 600, "Screen height should be at least 600");
    }

    [TestMethod]
    public void IsWindowValidOnPrimaryMonitor_FullyVisible_ReturnsTrue()
    {
        // Arrange - window at (100, 100) with size (800, 600)
        // This should be fully visible on any modern monitor
        double left = 100;
        double top = 100;
        double width = 800;
        double height = 600;

        // Act
        var result = MonitorHelper.IsWindowValidOnPrimaryMonitor(left, top, width, height);

        // Assert
        Assert.IsTrue(result, "Window at (100, 100) with size 800x600 should be valid");
    }

    [TestMethod]
    public void IsWindowValidOnPrimaryMonitor_PartiallyVisible_ReturnsTrue()
    {
        // Arrange - window partially off-screen but >50% visible
        // This test may vary depending on monitor resolution
        var (_, _, screenWidth, screenHeight) = MonitorHelper.GetPrimaryMonitorWorkArea();
        
        // Position window so it's partially off the right edge but >50% visible
        double left = screenWidth - 100; // 100 pixels from right edge
        double top = 100;
        double width = 150; // Only 100 pixels visible horizontally
        double height = 600;

        // Act
        var result = MonitorHelper.IsWindowValidOnPrimaryMonitor(left, top, width, height);

        // Assert - 100/150 = 66% visible, should be valid
        Assert.IsTrue(result, "Window with >50% visible should be valid");
    }

    [TestMethod]
    public void IsWindowValidOnPrimaryMonitor_MostlyOffScreen_ReturnsFalse()
    {
        // Arrange - window mostly off-screen (<50% visible)
        var (_, _, screenWidth, _) = MonitorHelper.GetPrimaryMonitorWorkArea();
        
        // Position window so only a small portion is visible
        double left = screenWidth - 10; // Only 10 pixels visible
        double top = 100;
        double width = 800; // Most of the window is off-screen
        double height = 600;

        // Act
        var result = MonitorHelper.IsWindowValidOnPrimaryMonitor(left, top, width, height);

        // Assert - only 10/800 = 1.25% visible, should be invalid
        Assert.IsFalse(result, "Window with <50% visible should be invalid");
    }

    [TestMethod]
    public void IsWindowValidOnPrimaryMonitor_CompletelyOffScreen_ReturnsFalse()
    {
        // Arrange - window completely off-screen
        var (_, _, screenWidth, screenHeight) = MonitorHelper.GetPrimaryMonitorWorkArea();
        
        double left = screenWidth + 1000; // Way off to the right
        double top = screenHeight + 1000; // Way down
        double width = 800;
        double height = 600;

        // Act
        var result = MonitorHelper.IsWindowValidOnPrimaryMonitor(left, top, width, height);

        // Assert
        Assert.IsFalse(result, "Window completely off-screen should be invalid");
    }

    [TestMethod]
    public void EnsureValidWindowPosition_ValidPosition_ReturnsOriginal()
    {
        // Arrange
        double left = 100;
        double top = 100;
        double width = 800;
        double height = 600;

        // Act
        var (x, y) = MonitorHelper.EnsureValidWindowPosition(left, top, width, height);

        // Assert
        Assert.AreEqual((int)left, x, "Valid position should not be modified");
        Assert.AreEqual((int)top, y, "Valid position should not be modified");
    }

    [TestMethod]
    public void EnsureValidWindowPosition_InvalidPosition_ReturnsCentered()
    {
        // Arrange - position way off-screen
        double left = 50000;
        double top = 50000;
        double width = 800;
        double height = 600;

        // Act
        var (x, y) = MonitorHelper.EnsureValidWindowPosition(left, top, width, height);

        // Assert - should return centered position, not the original
        Assert.AreNotEqual((int)left, x, "Invalid position should be modified");
        Assert.AreNotEqual((int)top, y, "Invalid position should be modified");
        
        // Verify it's actually centered (approximately)
        var (_, _, screenWidth, screenHeight) = MonitorHelper.GetPrimaryMonitorWorkArea();
        var expectedX = (screenWidth - (int)width) / 2;
        var expectedY = (screenHeight - (int)height) / 2;
        
        Assert.AreEqual(expectedX, x, "X should be centered");
        Assert.AreEqual(expectedY, y, "Y should be centered");
    }
}
