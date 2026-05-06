using System.Security.Cryptography;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
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

    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly ILlmProviderSelector _providerSelector;
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
        string? agentsFilePath = null,
        string? backupDir = null,
        string? defaultRulesPath = null)
    {
        _fileService = fileService;
        _settingsService = settingsService;
        _providerSelector = providerSelector;
        _defaultRulesPath = defaultRulesPath;

        var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _agentsFilePath = agentsFilePath ?? Path.Combine(docsPath, AgentsFileName);
        _backupDir = backupDir ?? Path.Combine(docsPath, "SingletonNotepad", "backups");

        _ = EnsureAgentsFileExistsAsync();
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
        var lineCount = content.Split('\n').Length;
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
        var result = new NormalizationResult
        {
            OriginalContent = content,
            ProviderName = _providerSelector.CurrentName,
        };

        result.BackupPath = await BackupContentAsync(content, ct);
        var rules = await LoadRulesAsync(ct);
        var prompt = BuildPrompt(rules.Content, content);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var normalizedContent = await _providerSelector.Current.CompleteAsync(prompt, ct);
        sw.Stop();

        result.NormalizedContent = normalizedContent;
        result.Duration = sw.Elapsed;

        var differ = new SideBySideDiffBuilder(new Differ());
        result.Diff = differ.BuildDiffModel(content, normalizedContent);

        foreach (var line in result.Diff.OldText.Lines)
        {
            if (line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted) result.LinesDeleted++;
            else if (line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Modified) result.LinesModified++;
        }
        foreach (var line in result.Diff.NewText.Lines)
        {
            if (line.Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted) result.LinesAdded++;
        }

        _lastNormalizeTime = DateTime.UtcNow;
        _lastNormalizedHash = ComputeSha256(content);
        return result;
    }

    private static string BuildPrompt(string rules, string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a Markdown normalization assistant.");
        sb.AppendLine("Restructure and reorganize the following content according to these rules:");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(rules))
            sb.AppendLine(rules);
        else
        {
            sb.AppendLine("- Maintain clear heading hierarchy (H1 -> H2 -> H3)");
            sb.AppendLine("- Group related content under appropriate headings");
            sb.AppendLine("- Remove redundant whitespace and empty lines");
            sb.AppendLine("- Preserve all meaningful content");
            sb.AppendLine("- Use consistent list formatting");
        }
        sb.AppendLine();
        sb.AppendLine("Return ONLY the normalized Markdown content, no explanations.");
        sb.AppendLine();
        sb.AppendLine("--- CONTENT TO NORMALIZE ---");
        sb.AppendLine(content);
        sb.AppendLine("--- END CONTENT ---");
        return sb.ToString();
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

    private async Task EnsureAgentsFileExistsAsync()
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