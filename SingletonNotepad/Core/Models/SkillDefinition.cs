namespace SingletonNotepad.Core.Models;

public class SkillDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public bool Always { get; set; }
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    // Cached lowercase values for hot-path matching in SelectForContext
    public string NameLower { get; private set; } = string.Empty;
    public IReadOnlyList<string> TagsLower { get; private set; } = [];
    public IReadOnlyList<string> DescriptionWords { get; private set; } = [];

    public void FinalizeCache()
    {
        NameLower = Name.ToLowerInvariant();
        TagsLower = Tags.Select(t => t.ToLowerInvariant()).ToList();
        DescriptionWords = Description
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Select(w => w.ToLowerInvariant())
            .ToArray();
    }
}
