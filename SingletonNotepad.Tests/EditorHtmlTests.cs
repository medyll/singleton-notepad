using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SingletonNotepad.Tests;

[TestClass]
public class EditorHtmlTests
{
    private static string EditorHtmlPath => Path.Combine(
        FindProjectRoot(),
        "SingletonNotepad", "Assets", "Editor", "editor.html");

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "SingletonNotepad")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find project root");
    }

    [TestMethod]
    public void Editor_UsesMinHeight_NotFixedHeight()
    {
        var html = File.ReadAllText(EditorHtmlPath);

        // #editor should use min-height, not height
        var editorRule = Regex.Match(html, @"#editor\s*\{([^}]+)\}", RegexOptions.Singleline);
        Assert.IsTrue(editorRule.Success, "#editor CSS rule not found");

        var styles = editorRule.Groups[1].Value;
        Assert.IsTrue(styles.Contains("min-height"), "#editor should use min-height");
        Assert.IsFalse(Regex.IsMatch(styles, @"(?<!min-)height\s*:"), "#editor should NOT have fixed height");
    }

    [TestMethod]
    public void Editor_OverflowY_IsAuto()
    {
        var html = File.ReadAllText(EditorHtmlPath);

        var editorRule = Regex.Match(html, @"#editor\s*\{([^}]+)\}", RegexOptions.Singleline);
        Assert.IsTrue(editorRule.Success, "#editor CSS rule not found");

        var styles = editorRule.Groups[1].Value;
        Assert.IsTrue(styles.Contains("overflow-y: auto"), "#editor should have overflow-y: auto");
    }

    [TestMethod]
    public void Editor_NoScrollbarWhenEmpty_CombinedRules()
    {
        var html = File.ReadAllText(EditorHtmlPath);

        var editorRule = Regex.Match(html, @"#editor\s*\{([^}]+)\}", RegexOptions.Singleline);
        Assert.IsTrue(editorRule.Success, "#editor CSS rule not found");

        var styles = editorRule.Groups[1].Value;

        // min-height + overflow-y: auto = scrollbar only when content overflows
        Assert.IsTrue(styles.Contains("min-height"), "Needs min-height for flexible sizing");
        Assert.IsTrue(styles.Contains("overflow-y: auto"), "Needs overflow-y: auto to hide scrollbar when empty");
    }

    [TestMethod]
    public void ProseMirror_MinHeight_FillsEditor()
    {
        var html = File.ReadAllText(EditorHtmlPath);

        var proseRule = Regex.Match(html, @"\.ProseMirror\s*\{([^}]+)\}", RegexOptions.Singleline);
        Assert.IsTrue(proseRule.Success, ".ProseMirror CSS rule not found");

        var styles = proseRule.Groups[1].Value;
        Assert.IsTrue(styles.Contains("min-height"), ".ProseMirror should have min-height to fill editor");
    }
}
