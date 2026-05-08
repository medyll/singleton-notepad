using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Tests;

[TestClass]
public class LanguageDetectorTests
{
    [TestMethod]
    public void Detect_FrenchContent_ReturnsFrFR()
    {
        var content = "Le chat est sur la table. Il mange du poisson et boit de l'eau. C'est un très bon animal.";
        Assert.AreEqual("fr-FR", LanguageDetector.Detect(content));
    }

    [TestMethod]
    public void Detect_EnglishContent_ReturnsEnUS()
    {
        var content = "The cat is on the table. It eats fish and drinks water. This is a very good animal.";
        Assert.AreEqual("en-US", LanguageDetector.Detect(content));
    }

    [TestMethod]
    public void Detect_EmptyContent_ReturnsAuto()
    {
        Assert.AreEqual("auto", LanguageDetector.Detect(""));
        Assert.AreEqual("auto", LanguageDetector.Detect("   "));
    }

    [TestMethod]
    public void Detect_ShortContent_ReturnsAuto()
    {
        Assert.AreEqual("auto", LanguageDetector.Detect("hello world"));
    }

    [TestMethod]
    public void Detect_MixedContent_ReturnsDominantLanguage()
    {
        // Mixed content with more English stop words
        var content = "Le chat is on the table. Il mange fish and drinks water.";
        var result = LanguageDetector.Detect(content);
        Assert.IsTrue(result == "en-US" || result == "auto");
    }
}
