using System.Text.RegularExpressions;
using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public interface ISkillService
{
    IReadOnlyList<SkillDefinition> LoadedSkills { get; }
    Task ScanAsync(string skillsPath, CancellationToken ct = default);
    IReadOnlyList<SkillDefinition> SelectForContext(string userMessage, string? documentContext);
    SkillDefinition? GetByName(string name);
}

public class SkillService : ISkillService, IDisposable
{
    private List<SkillDefinition> _skills = [];
    private FileSystemWatcher? _watcher;
    private System.Threading.Timer? _debounceTimer;
    private string _currentPath = string.Empty;

    public IReadOnlyList<SkillDefinition> LoadedSkills => _skills;

    public async Task ScanAsync(string skillsPath, CancellationToken ct = default)
    {
        _currentPath = skillsPath;

        if (string.IsNullOrWhiteSpace(skillsPath) || !Directory.Exists(skillsPath))
        {
            _skills = [];
            SetupWatcher(skillsPath);
            return;
        }

        var loaded = new List<SkillDefinition>();

        foreach (var file in Directory.GetFiles(skillsPath, "*.md", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var text = await File.ReadAllTextAsync(file, ct);
                var skill = ParseSkillFile(text, file);
                if (skill != null)
                    loaded.Add(skill);
                else
                    System.Diagnostics.Debug.WriteLine($"[SkillService] No valid frontmatter in: {file}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkillService] Error reading {file}: {ex.Message}");
            }
        }

        _skills = loaded;
        SetupWatcher(skillsPath);
    }

    public IReadOnlyList<SkillDefinition> SelectForContext(string userMessage, string? documentContext)
    {
        var combined = (userMessage + " " + (documentContext ?? "")).ToLowerInvariant();

        var result = new List<(SkillDefinition skill, int score)>();

        foreach (var skill in _skills)
        {
            if (skill.Always)
            {
                result.Add((skill, int.MaxValue));
                continue;
            }

            int score = 0;
            foreach (var tag in skill.Tags)
            {
                if (combined.Contains(tag.ToLowerInvariant()))
                    score += 2;
            }

            var nameL = skill.Name.ToLowerInvariant();
            if (combined.Contains(nameL))
                score += 3;

            foreach (var word in skill.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length > 4 && combined.Contains(word.ToLowerInvariant()))
                    score += 1;
            }

            if (score >= 2)
                result.Add((skill, score));
        }

        return result
            .OrderByDescending(x => x.score)
            .Take(3)
            .Select(x => x.skill)
            .ToList();
    }

    public SkillDefinition? GetByName(string name)
        => _skills.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    private static SkillDefinition? ParseSkillFile(string text, string filePath)
    {
        // Expect frontmatter between first --- and second ---
        if (!text.StartsWith("---")) return null;

        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0) return null;

        var frontmatter = text[3..end].Trim();
        var body = text[(end + 4)..].TrimStart('\r', '\n');

        var skill = new SkillDefinition { FilePath = filePath, Content = body };

        foreach (var line in frontmatter.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("name:"))
                skill.Name = trimmed[5..].Trim();
            else if (trimmed.StartsWith("description:"))
                skill.Description = trimmed[12..].Trim();
            else if (trimmed.StartsWith("always:"))
            {
                var val = trimmed[7..].Trim().ToLowerInvariant();
                skill.Always = val == "true";
            }
            else if (trimmed.StartsWith("tags:"))
            {
                // Inline: tags: [a, b, c]
                var tagsLine = trimmed[5..].Trim();
                var match = Regex.Match(tagsLine, @"\[([^\]]*)\]");
                if (match.Success)
                {
                    skill.Tags = match.Groups[1].Value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim().Trim('"', '\''))
                        .Where(t => t.Length > 0)
                        .ToList();
                }
            }
            else if (trimmed.StartsWith("- ") && skill.Tags.Count == 0)
            {
                // Multi-line tags list
                skill.Tags.Add(trimmed[2..].Trim().Trim('"', '\''));
            }
        }

        if (string.IsNullOrWhiteSpace(skill.Name)) return null;

        return skill;
    }

    private void SetupWatcher(string path)
    {
        _watcher?.Dispose();
        _watcher = null;

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        _watcher = new FileSystemWatcher(path, "*.md")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Created += OnFolderChanged;
        _watcher.Deleted += OnFolderChanged;
        _watcher.Changed += OnFolderChanged;
    }

    private void OnFolderChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(
            _ => _ = ScanAsync(_currentPath),
            null,
            TimeSpan.FromMilliseconds(500),
            Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _watcher?.Dispose();
    }
}
