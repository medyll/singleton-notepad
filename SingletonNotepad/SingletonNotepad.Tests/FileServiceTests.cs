using Microsoft.VisualStudio.TestTools.UnitTesting;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class FileServiceTests
{
    private IFileService _fileService = null!;
    private string _testFilePath = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _fileService = new FileService();
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_singleton_{Guid.NewGuid()}.md");
        _fileService.FilePath = _testFilePath;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [TestMethod]
    public async Task LoadAsync_CreatesFile_WhenNotExists()
    {
        // Arrange
        Assert.IsFalse(File.Exists(_testFilePath));

        // Act
        var content = await _fileService.LoadAsync();

        // Assert
        Assert.IsTrue(File.Exists(_testFilePath));
        Assert.IsNotNull(content);
        Assert.IsTrue(content.Length > 0);
    }

    [TestMethod]
    public async Task SaveAsync_WritesContent_ToFile()
    {
        // Arrange
        var expectedContent = "# Test Note\n\nThis is test content.";

        // Act
        await _fileService.SaveAsync(expectedContent);
        var actualContent = await _fileService.LoadAsync();

        // Assert
        Assert.AreEqual(expectedContent, actualContent);
    }

    [TestMethod]
    public async Task SaveAsync_And_LoadAsync_RoundTrip()
    {
        // Arrange
        var originalContent = "# Round Trip Test\n\nTesting save and load.";

        // Act
        await _fileService.SaveAsync(originalContent);
        var loadedContent = await _fileService.LoadAsync();

        // Assert
        Assert.AreEqual(originalContent, loadedContent);
    }
}
