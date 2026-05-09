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
    private volatile List<SkillDefinition> _skills = [];
    private FileSystemWatcher? _watcher;
    private System.Threading.Timer? _debounceTimer;
    private string _watcherPath = string.Empty;
    private readonly object _scanLock = new();

    public IReadOnlyList<SkillDefinition> LoadedSkills => _skills;

    public async Task ScanAsync(string skillsPath, CancellationToken ct = default)
    {
        bool invalidPath;
        lock (_scanLock)
        {
            invalidPath = string.IsNullOrWhiteSpace(skillsPath) || !Directory.Exists(skillsPath);
            if (invalidPath) _skills = [];
        }

        if (invalidPath)
        {
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

        lock (_scanLock) { _skills = loaded; }
        SetupWatcher(skillsPath);
    }

    public IReadOnlyList<SkillDefinition> SelectForContext(string userMessage, string? documentContext)
    {
        var combined = (userMessage + " " + (documentContext ?? "")).ToLowerInvariant();
        var snapshot = _skills;

        var result = new List<(SkillDefinition skill, int score)>();

        foreach (var skill in snapshot)
        {
            if (skill.Always)
            {
                result.Add((skill, int.MaxValue));
                continue;
            }

            int score = 0;
            foreach (var tag in skill.TagsLower)
            {
                if (combined.Contains(tag))
                    score += 2;
            }

            if (combined.Contains(skill.NameLower))
                score += 3;

            foreach (var word in skill.DescriptionWords)
            {
                if (combined.Contains(word))
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
    {
        var snapshot = _skills;
        return snapshot.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static SkillDefinition? ParseSkillFile(string text, string filePath)
    {
        if (!text.StartsWith("---")) return null;

        var end = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0) return null;

        var frontmatter = text[3..end].Trim();
        var body = text[(end + 4)..].TrimStart('\r', '\n');

        var skill = new SkillDefinition { FilePath = filePath, Content = body };

        bool inTagsBlock = false;
        foreach (var line in frontmatter.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("name:"))
            {
                skill.Name = trimmed[5..].Trim();
                inTagsBlock = false;
            }
            else if (trimmed.StartsWith("description:"))
            {
                skill.Description = trimmed[12..].Trim();
                inTagsBlock = false;
            }
            else if (trimmed.StartsWith("always:"))
            {
                skill.Always = trimmed[7..].Trim().ToLowerInvariant() == "true";
                inTagsBlock = false;
            }
            else if (trimmed.StartsWith("tags:"))
            {
                inTagsBlock = false;
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
                else if (string.IsNullOrEmpty(tagsLine))
                {
                    inTagsBlock = true;
                }
            }
            else if (trimmed.StartsWith("- ") && inTagsBlock)
            {
                skill.Tags.Add(trimmed[2..].Trim().Trim('"', '\''));
            }
            else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('-'))
            {
                inTagsBlock = false;
            }
        }

        if (string.IsNullOrWhiteSpace(skill.Name)) return null;

        skill.FinalizeCache();
        return skill;
    }

    private void SetupWatcher(string path)
    {
        // Skip teardown/recreate when watcher-triggered rescans call back with the same path —
        // rebuilding the watcher mid-debounce would reset the timer and miss rapid bursts.
        if (_watcherPath == path && _watcher != null) return;

        _watcher?.Dispose();
        _watcher = null;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        _watcherPath = path;

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        // Timer is created once per path and reused via Change() — avoids alloc on every file event.
        _debounceTimer = new System.Threading.Timer(
            _ => ScanAsync(path).ContinueWith(
                t => System.Diagnostics.Debug.WriteLine($"[SkillService] Rescan error: {t.Exception?.Flatten().Message}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default),
            null, Timeout.Infinite, Timeout.Infinite);

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
        _debounceTimer?.Change(500, Timeout.Infinite);
    }

    public static void AppendSkillsBlock(System.Text.StringBuilder sb, IReadOnlyList<SkillDefinition> skills)
    {
        if (skills.Count == 0) return;
        sb.AppendLine("[SKILLS ACTIVES]");
        foreach (var skill in skills)
        {
            sb.AppendLine($"--- skill: {skill.Name} ---");
            sb.AppendLine(skill.Content.Trim());
            sb.AppendLine("--- fin skill ---");
        }
        sb.AppendLine();
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _watcher?.Dispose();
    }
}
