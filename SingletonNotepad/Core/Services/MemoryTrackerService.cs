using System.Text;
using SingletonNotepad.Core.Models;

namespace SingletonNotepad.Core.Services;

public class MemoryTrackerService : IMemoryTrackerService
{
    private const string MemoryFileName = "NOTEPAD_SINGLETON_MEMORY.md";
    private readonly string _memoryFilePath;
    private readonly ISettingsService _settingsService;

    public MemoryTrackerService(ISettingsService settingsService, string? memoryFilePath = null)
    {
        _settingsService = settingsService;

        var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _memoryFilePath = memoryFilePath ?? Path.Combine(docsPath, MemoryFileName);
    }

    public async Task AppendAsync(ChangeRecord record, CancellationToken ct = default)
    {
        var line = FormatRecord(record);

        if (!File.Exists(_memoryFilePath))
        {
            var header = "# Singleton Notepad — Memory Log\n\n" +
                         "| Date | Type | Lines Changed | Preview | Backup |\n" +
                         "|------|------|---------------|---------|--------|\n\n";
            await File.WriteAllTextAsync(_memoryFilePath, header, ct);
        }

        await File.AppendAllTextAsync(_memoryFilePath, line + Environment.NewLine, ct);
    }

    public async Task<IReadOnlyList<ChangeRecord>> GetHistoryAsync(int maxRecords = 50, CancellationToken ct = default)
    {
        if (!File.Exists(_memoryFilePath))
        {
            return Array.Empty<ChangeRecord>();
        }

        var lines = await File.ReadAllLinesAsync(_memoryFilePath, ct);
        var records = new List<ChangeRecord>();

        // Skip header lines (first 4 lines: title, blank, header row, separator)
        for (int i = 4; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("|"))
            {
                continue;
            }

            var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 5)
            {
                records.Add(new ChangeRecord
                {
                    Timestamp = ParseTimestamp(parts[0]),
                    Type = parts[1],
                    LinesChanged = ParseLinesChanged(parts[2]),
                    Preview = parts[3],
                    BackupPath = parts[4],
                });
            }

            if (records.Count >= maxRecords)
            {
                break;
            }
        }

        return records.AsReadOnly();
    }

    private static string FormatRecord(ChangeRecord record)
    {
        var preview = EscapeMarkdown(record.Preview);
        var backup = string.IsNullOrEmpty(record.BackupPath) ? "—" : Path.GetFileName(record.BackupPath);
        var linesChanged = record.LinesChanged > 0 ? $"+{record.LinesChanged}" : $"{record.LinesChanged}";

        return $"| {record.Timestamp:yyyy-MM-dd HH:mm} | {record.Type} | {linesChanged} | {preview} | {backup} |";
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "—";
        }

        var maxLen = Math.Min(text.Length, 60);
        var preview = text[..maxLen].Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
        return preview.Length < text.Length ? preview + "…" : preview;
    }

    private static DateTime ParseTimestamp(string text)
    {
        if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var dt))
        {
            return dt;
        }
        return DateTime.Now;
    }

    private static int ParseLinesChanged(string text)
    {
        var cleaned = text.Replace("+", "").Replace("-", "");
        return int.TryParse(cleaned, out var n) ? n : 0;
    }
}
