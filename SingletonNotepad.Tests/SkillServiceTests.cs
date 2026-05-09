using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class SkillServiceTests
{
    private string _testDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "skill-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    private static string ValidSkillMd(string name, string description, string[] tags, bool always = false, string body = "You are an expert.")
    {
        var tagsLine = "[" + string.Join(", ", tags) + "]";
        var alwaysLine = always ? "\nalways: true" : string.Empty;
        return $"---\nname: {name}\ndescription: {description}\ntags: {tagsLine}{alwaysLine}\n---\n{body}";
    }

    [TestMethod]
    public async Task ScanAsync_LoadsValidSkills()
    {
        File.WriteAllText(Path.Combine(_testDir, "finance.md"),
            ValidSkillMd("finance", "Financial analysis", ["bilan", "finance"]));

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        Assert.AreEqual(1, svc.LoadedSkills.Count);
        Assert.AreEqual("finance", svc.LoadedSkills[0].Name);
    }

    [TestMethod]
    public async Task ScanAsync_IgnoresFilesWithoutFrontmatter()
    {
        File.WriteAllText(Path.Combine(_testDir, "nofm.md"), "# Just a heading\nNo frontmatter here.");

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        Assert.AreEqual(0, svc.LoadedSkills.Count);
    }

    [TestMethod]
    public async Task ScanAsync_EmptyPath_LoadsZeroSkills()
    {
        var svc = new SkillService();
        await svc.ScanAsync(string.Empty);

        Assert.AreEqual(0, svc.LoadedSkills.Count);
    }

    [TestMethod]
    public async Task ScanAsync_NonExistentPath_LoadsZeroSkills()
    {
        var svc = new SkillService();
        await svc.ScanAsync(@"C:\path\that\does\not\exist\ever");

        Assert.AreEqual(0, svc.LoadedSkills.Count);
    }

    [TestMethod]
    public async Task SelectForContext_ReturnsAlwaysSkillsRegardlessOfScore()
    {
        File.WriteAllText(Path.Combine(_testDir, "persona.md"),
            ValidSkillMd("persona", "Global persona", ["persona"], always: true));

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        var selected = svc.SelectForContext("Hello world", null);

        Assert.AreEqual(1, selected.Count);
        Assert.AreEqual("persona", selected[0].Name);
    }

    [TestMethod]
    public async Task SelectForContext_ReturnsMatchingSkillsByScore()
    {
        File.WriteAllText(Path.Combine(_testDir, "finance.md"),
            ValidSkillMd("finance", "Financial analysis", ["bilan", "finance"]));
        File.WriteAllText(Path.Combine(_testDir, "writing.md"),
            ValidSkillMd("writing", "Writing helper", ["redaction", "writing"]));

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        var selected = svc.SelectForContext("Analyse mon bilan financier", null);

        Assert.IsTrue(selected.Any(s => s.Name == "finance"));
        Assert.IsFalse(selected.Any(s => s.Name == "writing"));
    }

    [TestMethod]
    public async Task SelectForContext_ReturnsMaxThreeSkills()
    {
        for (int i = 1; i <= 5; i++)
        {
            File.WriteAllText(Path.Combine(_testDir, $"s{i}.md"),
                ValidSkillMd($"skill{i}", $"Helper for keyword{i}", [$"keyword{i}"]));
        }

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        // Message contains all keywords so all would score >= 2
        var msg = "keyword1 keyword2 keyword3 keyword4 keyword5";
        var selected = svc.SelectForContext(msg, null);

        Assert.IsTrue(selected.Count <= 3);
    }

    [TestMethod]
    public async Task SelectForContext_NoMatch_ReturnsEmpty()
    {
        File.WriteAllText(Path.Combine(_testDir, "finance.md"),
            ValidSkillMd("finance", "Financial analysis", ["bilan", "finance"]));

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        var selected = svc.SelectForContext("Bonjour comment allez vous", null);

        Assert.AreEqual(0, selected.Count);
    }

    [TestMethod]
    public async Task GetByName_FindsSkillCaseInsensitive()
    {
        File.WriteAllText(Path.Combine(_testDir, "finance.md"),
            ValidSkillMd("Finance", "Financial analysis", ["bilan"]));

        var svc = new SkillService();
        await svc.ScanAsync(_testDir);

        Assert.IsNotNull(svc.GetByName("finance"));
        Assert.IsNotNull(svc.GetByName("FINANCE"));
        Assert.IsNull(svc.GetByName("writing"));
    }
}
