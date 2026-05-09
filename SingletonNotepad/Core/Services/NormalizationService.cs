using System.Security.Cryptography;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;

namespace SingletonNotepad.Core.Services;

public class NormalizationService : INormalizationService
{
    private const string AgentsFileName = "NOTEPAD_SINGLETON_AGENTS.md";
    private const int MaxFileSizeBytes = 500 * 1024;
    private const int MaxLineCount = 10_000;
    private const int MinNormalizeIntervalMinutes = 5;
    private const int DefaultMaxBackupCount = 10;
    private const string TagNormalisation = "normalisation";
    private const string TagWriting = "writing";

    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly ILlmProviderSelector _providerSelector;
    private readonly ISkillService? _skillService;
    private readonly string _agentsFilePath;
    private readonly string _backupDir;
    private readonly string? _defaultRulesPath;
    private DateTime _lastNormalizeTime;
    private string _lastNormalizedHash = string.Empty;

    public string RulesFilePath => _agentsFilePath;

    public NormalizationService(
        IFileService fileService,
        ISettingsService settingsService,
        ILlmProviderSelector providerSelector,
        ISkillService? skillService = null,
        string? agentsFilePath = null,
        string? backupDir = null,
        string? defaultRulesPath = null)
    {
        _fileService = fileService;
        _settingsService = settingsService;
        _providerSelector = providerSelector;
        _skillService = skillService;
        _defaultRulesPath = defaultRulesPath;

        var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _agentsFilePath = agentsFilePath ?? Path.Combine(docsPath, AgentsFileName);
        _backupDir = backupDir ?? Path.Combine(docsPath, "SingletonNotepad", "backups");

        EnsureAgentsFileExists();
    }

    public async Task<NormalizationRule> LoadRulesAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_agentsFilePath))
            return new NormalizationRule { Content = string.Empty, FilePath = _agentsFilePath };

        var content = await File.ReadAllTextAsync(_agentsFilePath, ct);
        return new NormalizationRule { Content = content, FilePath = _agentsFilePath };
    }

    public bool IsRateLimited(string content, out TimeSpan remaining)
    {
        var elapsed = DateTime.UtcNow - _lastNormalizeTime;
        if (elapsed < TimeSpan.FromMinutes(MinNormalizeIntervalMinutes))
        {
            var contentHash = ComputeSha256(content);
            if (contentHash == _lastNormalizedHash)
            {
                remaining = TimeSpan.FromMinutes(MinNormalizeIntervalMinutes) - elapsed;
                return true;
            }
        }
        remaining = TimeSpan.Zero;
        return false;
    }

    public bool ExceedsSizeLimit(string content, out string reason)
    {
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxFileSizeBytes)
        {
            reason = $"File size ({byteCount / 1024} KB) exceeds {MaxFileSizeBytes / 1024} KB limit.";
            return true;
        }
        var lineCount = CountLines(content);
        if (lineCount > MaxLineCount)
        {
            reason = $"Line count ({lineCount:N0}) exceeds {MaxLineCount:N0} limit.";
            return true;
        }
        reason = string.Empty;
        return false;
    }

    public async Task<NormalizationResult> NormalizeAsync(string content, CancellationToken ct = default)
    {
        var activeSettings = await _settingsService.LoadAsync(ct);
        var provider = _providerSelector.GetProvider(activeSettings.LlmProvider) ?? _providerSelector.Current;

        var result = new NormalizationResult
        {
            OriginalContent = content,
            ProviderName = provider.Name,
        };

        result.BackupPath = await BackupContentAsync(content, ct);

        var rules = await LoadRulesAsync(ct);

        // Inject skills tagged for writing/normalisation so user-defined personas apply to every normalize run.
        // TagsLower is pre-lowercased at parse time, so Contains() avoids OrdinalIgnoreCase overhead here.
        var injectedSkills = _skillService != null
            ? _skillService.LoadedSkills
                .Where(s => s.Always || s.TagsLower.Contains(TagNormalisation) || s.TagsLower.Contains(TagWriting))
                .ToList()
            : [];

        var (systemPrompt, userMessage) = BuildPrompt(rules.Content, content, injectedSkills);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rawResponse = await provider.CompleteAsync(systemPrompt, userMessage, ct);
        sw.Stop();

        var normalizedContent = CleanLlmResponse(rawResponse);
        result.NormalizedContent = normalizedContent;
        result.Duration = sw.Elapsed;

        result.DiffJson = BuildDiffPayload(content, normalizedContent, out var added, out var deleted, out var modified);
        result.LinesAdded = added;
        result.LinesDeleted = deleted;
        result.LinesModified = modified;

        _lastNormalizeTime = DateTime.UtcNow;
        _lastNormalizedHash = ComputeSha256(content);
        return result;
    }

    private static DiffPayload BuildDiffPayload(string original, string normalized, out int added, out int deleted, out int modified)
    {
        var differ = new SideBySideDiffBuilder(new Differ());
        var diff = differ.BuildDiffModel(original, normalized);

        var payload = new DiffPayload();
        added = 0;
        deleted = 0;
        modified = 0;

        var oldLines = diff.OldText.Lines;
        var newLines = diff.NewText.Lines;

        int i = 0, j = 0;
        var currentHunk = new DiffHunk();
        var hunkHasChanges = false;
        var pendingContext = new List<string>();

        while (i < oldLines.Count || j < newLines.Count)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = j < newLines.Count ? newLines[j] : null;

            if (oldLine?.Type == ChangeType.Imaginary && newLine != null)
            {
                if (newLine.Type == ChangeType.Inserted)
                {
                    if (!hunkHasChanges)
                    {
                        FlushPendingContext(currentHunk, pendingContext);
                        hunkHasChanges = true;
                    }
                    currentHunk.Changes.Add(new DiffChange { Kind = "ins", Text = newLine.Text });
                    added++;
                }
                j++;
            }
            else if (newLine?.Type == ChangeType.Imaginary && oldLine != null)
            {
                if (oldLine.Type == ChangeType.Deleted)
                {
                    if (!hunkHasChanges)
                    {
                        FlushPendingContext(currentHunk, pendingContext);
                        hunkHasChanges = true;
                    }
                    currentHunk.Changes.Add(new DiffChange { Kind = "del", Text = oldLine.Text });
                    deleted++;
                }
                i++;
            }
            else if (oldLine?.Type == ChangeType.Modified && newLine?.Type == ChangeType.Modified)
            {
                if (!hunkHasChanges)
                {
                    FlushPendingContext(currentHunk, pendingContext);
                    hunkHasChanges = true;
                }
                var wordDiff = ComputeWordDiff(oldLine.Text, newLine.Text);
                currentHunk.Changes.Add(new DiffChange
                {
                    Kind = "mod",
                    Old = oldLine.Text,
                    New = newLine.Text,
                    OldWords = wordDiff.OldWords,
                    NewWords = wordDiff.NewWords,
                });
                modified++;
                i++;
                j++;
            }
            else if (oldLine?.Type == ChangeType.Unchanged && newLine?.Type == ChangeType.Unchanged)
            {
                if (hunkHasChanges)
                {
                    currentHunk.ContextAfter.Add(oldLine.Text);
                    if (currentHunk.ContextAfter.Count >= 3)
                    {
                        payload.Hunks.Add(currentHunk);
                        currentHunk = new DiffHunk();
                        hunkHasChanges = false;
                    }
                }
                else
                {
                    pendingContext.Add(oldLine.Text);
                    if (pendingContext.Count > 3)
                        pendingContext.RemoveAt(0);
                }
                i++;
                j++;
            }
            else
            {
                i++;
                j++;
            }
        }

        if (hunkHasChanges && currentHunk.Changes.Count > 0)
        {
            payload.Hunks.Add(currentHunk);
        }

        payload.Stats = new DiffStats { Added = added, Deleted = deleted, Modified = modified };
        return payload;
    }

    private static void FlushPendingContext(DiffHunk hunk, List<string> pending)
    {
        hunk.ContextBefore.AddRange(pending);
        pending.Clear();
    }

    private static (List<DiffWord> OldWords, List<DiffWord> NewWords) ComputeWordDiff(string oldText, string newText)
    {
        var oldTokens = Tokenize(oldText);
        var newTokens = Tokenize(newText);

        var differ = new InlineDiffBuilder(new Differ());
        var tokenDiff = differ.BuildDiffModel(string.Join("", oldTokens), string.Join("", newTokens));

        var oldResult = new List<DiffWord>();
        var newResult = new List<DiffWord>();

        foreach (var line in tokenDiff.Lines)
        {
            if (line.Type == ChangeType.Deleted || line.Type == ChangeType.Modified)
                oldResult.Add(new DiffWord { Text = line.Text, Changed = true });
            else if (line.Type == ChangeType.Inserted)
                newResult.Add(new DiffWord { Text = line.Text, Changed = true });
            else if (line.Type == ChangeType.Unchanged)
            {
                oldResult.Add(new DiffWord { Text = line.Text, Changed = false });
                newResult.Add(new DiffWord { Text = line.Text, Changed = false });
            }
        }

        return (oldResult, newResult);
    }

    private static string[] Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                tokens.Add(c.ToString());
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens.ToArray();
    }

    private static (string system, string user) BuildPrompt(string rules, string content, IReadOnlyList<SkillDefinition>? skills = null)
    {
        var system = new StringBuilder();

        if (skills != null)
            SkillService.AppendSkillsBlock(system, skills);

        system.AppendLine("Tu es un assistant de normalisation de notes Markdown.");
        system.AppendLine("Normalise le contenu qui te sera envoyé. Retourne UNIQUEMENT le contenu normalisé, sans explication ni commentaire.");
        system.AppendLine();

        if (!string.IsNullOrWhiteSpace(rules))
        {
            system.AppendLine("RÈGLES À APPLIQUER :");
            system.AppendLine(rules.Trim());
        }
        else
        {
            system.AppendLine("RÈGLES À APPLIQUER :");
            system.AppendLine("- Maintenir la hiérarchie des titres (H1 → H2 → H3)");
            system.AppendLine("- Regrouper les notes connexes sous les bons titres");
            system.AppendLine("- Corriger l'orthographe et la grammaire");
            system.AppendLine("- Supprimer les lignes vides multiples (max 1)");
            system.AppendLine("- Préserver tout le contenu existant");
            system.AppendLine("- Ne jamais inventer de contenu");
        }

        var escapedContent = EscapeXmlTags(content);
        var user = $"<contenu_a_normaliser>\n{escapedContent}\n</contenu_a_normaliser>";

        return (system.ToString(), user);
    }

    private static string CleanLlmResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return response;

        var text = response.Trim();

        // Si le modèle a renvoyé la balise XML, extraire uniquement ce qu'il y a dedans
        var openTag  = "<contenu_a_normaliser>";
        var closeTag = "</contenu_a_normaliser>";
        var openIdx  = text.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
        if (openIdx >= 0)
        {
            var closeIdx = text.IndexOf(closeTag, openIdx, StringComparison.OrdinalIgnoreCase);
            if (closeIdx > openIdx)
                return text[(openIdx + openTag.Length)..closeIdx].Trim();
        }

        // Strip delimiter blocks like "--- NORMALIZED CONTENT ---" ... "--- END CONTENT ---"
        // (preserve genuine markdown <hr> by requiring the CONTENT marker)
        var lines = text.Split('\n');
        var result = new List<string>(lines.Length);
        bool inDelimiterBlock = false;
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("---") && t.EndsWith("---") && t.Contains("CONTENT", StringComparison.OrdinalIgnoreCase))
            {
                inDelimiterBlock = !inDelimiterBlock;
                continue;
            }
            if (!inDelimiterBlock) result.Add(line);
        }

        return string.Join('\n', result).Trim();
    }

    private async Task<string> BackupContentAsync(string content, CancellationToken ct)
    {
        if (!Directory.Exists(_backupDir))
            Directory.CreateDirectory(_backupDir);

        var settings = await _settingsService.LoadAsync(ct);
        var maxBackups = settings.MaxBackupCount ?? DefaultMaxBackupCount;

        var existingBackups = Directory.GetFiles(_backupDir, "backup_*.md")
            .Select(f => new FileInfo(f))
            .Where(f => f.Exists)
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        var nextVersion = existingBackups.Count + 1;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var backupPath = Path.Combine(_backupDir, $"backup_{timestamp}_v{nextVersion}.md");
        await File.WriteAllTextAsync(backupPath, content, ct);

        var allBackups = Directory.GetFiles(_backupDir, "backup_*.md")
            .Select(f => new FileInfo(f))
            .Where(f => f.Exists)
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (allBackups.Count > maxBackups)
        {
            var toDelete = allBackups.Skip(maxBackups);
            foreach (var oldBackup in toDelete)
            {
                try { oldBackup.Delete(); } catch { }
            }
        }

        return backupPath;
    }

    public async Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_backupDir))
            return Array.Empty<BackupInfo>();

        var settings = await _settingsService.LoadAsync(ct);
        var maxBackups = settings.MaxBackupCount ?? DefaultMaxBackupCount;

        var backups = Directory.GetFiles(_backupDir, "backup_*.md")
            .Select(f => new FileInfo(f))
            .Where(f => f.Exists)
            .OrderByDescending(f => f.CreationTimeUtc)
            .Take(maxBackups)
            .Select((f, idx) => new BackupInfo
            {
                Path = f.FullName,
                Timestamp = f.CreationTimeUtc,
                Version = idx + 1,
                SizeBytes = f.Length,
            })
            .ToList();

        return backups.AsReadOnly();
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content)) return 0;
        int count = 1;
        for (int i = 0; i < content.Length; i++)
            if (content[i] == '\n') count++;
        return count;
    }

    private static string EscapeXmlTags(string content)
    {
        return content.Replace("</contenu_a_normaliser>", "&lt;/contenu_a_normaliser&gt;", StringComparison.OrdinalIgnoreCase)
                      .Replace("<contenu_a_normaliser>", "&lt;contenu_a_normaliser&gt;", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureAgentsFileExists()
    {
        if (File.Exists(_agentsFilePath)) return;
        var source = _defaultRulesPath;
        if (string.IsNullOrEmpty(source) || !File.Exists(source))
            source = Path.Combine(AppContext.BaseDirectory, "Resources", "DefaultRules.md");
        if (!string.IsNullOrEmpty(source) && File.Exists(source))
        {
            var dir = Path.GetDirectoryName(_agentsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(source, _agentsFilePath, overwrite: false);
        }
    }
}