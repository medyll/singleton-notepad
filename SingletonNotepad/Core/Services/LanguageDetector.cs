namespace SingletonNotepad.Core.Services;

/// <summary>
/// Simple language detection using stop-word frequency heuristic.
/// Supports French and English. Returns "auto" when inconclusive.
/// </summary>
public static class LanguageDetector
{
    private static readonly HashSet<string> FrenchWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "le", "la", "les", "de", "du", "des", "et", "est", "sont", "que", "qui",
        "dans", "pour", "avec", "sur", "pas", "ne", "se", "ce", "il", "elle",
        "nous", "vous", "ils", "elles", "mais", "ou", "donc", "comme", "tout",
        "faire", "être", "avoir", "dit", "une", "mon", "ton", "son", "notre",
        "votre", "leur", "au", "aux", "par", "plus", "bien", "aussi", "très",
        "peut", "faut", "cette", "ces", "mes", "tes", "ses", "entre", "sans",
        "sous", "deux", "trois", "premier", "après", "avant", "encore", "toujours"
    };

    private static readonly HashSet<string> EnglishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "must", "shall", "can", "need", "dare",
        "and", "but", "or", "nor", "for", "yet", "so", "in", "on", "at",
        "to", "of", "with", "by", "from", "up", "about", "into", "over",
        "after", "this", "that", "these", "those", "it", "its", "they",
        "them", "their", "there", "here", "where", "when", "what", "which",
        "who", "whom", "whose", "how", "not", "no", "all", "each", "every",
        "both", "few", "more", "most", "other", "some", "such", "only",
        "own", "same", "than", "too", "very", "just", "because", "as"
    };

    /// <summary>
    /// Detects language from content. Returns "fr-FR", "en-US", or "auto" if inconclusive.
    /// </summary>
    public static string Detect(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "auto";

        var words = content.Split((char[]?)null!, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '—', '–'))
            .Where(w => w.Length > 0)
            .Take(200)
            .ToList();

        if (words.Count < 3)
            return "auto";

        int frenchScore = 0;
        int englishScore = 0;

        foreach (var word in words)
        {
            var lower = word.ToLowerInvariant();
            if (FrenchWords.Contains(lower)) frenchScore++;
            if (EnglishWords.Contains(lower)) englishScore++;
        }

        var total = frenchScore + englishScore;
        if (total < 3)
            return "auto";

        var frenchRatio = (double)frenchScore / total;
        if (frenchRatio >= 0.6)
            return "fr-FR";
        if (frenchRatio <= 0.4)
            return "en-US";

        return "auto";
    }
}
