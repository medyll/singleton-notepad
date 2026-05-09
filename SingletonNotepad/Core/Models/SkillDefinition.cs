namespace SingletonNotepad.Core.Models;

public class SkillDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public bool Always { get; set; }
    public string Content { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
