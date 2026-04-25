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
        var originalContent = "# Round Trip Test\n\nTesting save and load.";
        await _fileService.SaveAsync(originalContent);
        var loadedContent = await _fileService.LoadAsync();
        Assert.AreEqual(originalContent, loadedContent);
    }

    [TestMethod]
    public async Task ScheduleSave_SavesContent_AfterDebounce()
    {
        var savedFired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fileService.FileSaved += () => savedFired.TrySetResult(true);

        _fileService.ScheduleSave("# Debounce Test");

        var completed = await Task.WhenAny(savedFired.Task, Task.Delay(3000));
        Assert.AreSame(savedFired.Task, completed, "FileSaved event not raised within 3s");

        var content = await _fileService.LoadAsync();
        Assert.AreEqual("# Debounce Test", content);
    }

    [TestMethod]
    public async Task ScheduleSave_ResetsTimer_OnRapidCalls()
    {
        var savedFired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _fileService.FileSaved += () => savedFired.TrySetResult(true);

        // Rapid calls — only last content should be saved
        _fileService.ScheduleSave("first");
        await Task.Delay(100);
        _fileService.ScheduleSave("second");
        await Task.Delay(100);
        _fileService.ScheduleSave("final");

        var completed = await Task.WhenAny(savedFired.Task, Task.Delay(4000));
        Assert.AreSame(savedFired.Task, completed, "FileSaved event not raised within 4s");

        var content = await _fileService.LoadAsync();
        Assert.AreEqual("final", content);
    }

    [TestMethod]
    public void Watch_ReturnsDisposable_WithoutThrowing()
    {
        using var watcher = _fileService.Watch(_ => { });
        Assert.IsNotNull(watcher);
    }
}
