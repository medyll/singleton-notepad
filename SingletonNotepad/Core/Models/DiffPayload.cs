using System.Text.Json.Serialization;

namespace SingletonNotepad.Core.Models;

public class DiffPayload
{
    [JsonPropertyName("hunks")]
    public List<DiffHunk> Hunks { get; set; } = [];

    [JsonPropertyName("stats")]
    public DiffStats Stats { get; set; } = new();
}

public class DiffHunk
{
    [JsonPropertyName("context_before")]
    public List<string> ContextBefore { get; set; } = [];

    [JsonPropertyName("changes")]
    public List<DiffChange> Changes { get; set; } = [];

    [JsonPropertyName("context_after")]
    public List<string> ContextAfter { get; set; } = [];
}

public class DiffChange
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "context";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("old")]
    public string? Old { get; set; }

    [JsonPropertyName("new")]
    public string? New { get; set; }

    [JsonPropertyName("old_words")]
    public List<DiffWord>? OldWords { get; set; }

    [JsonPropertyName("new_words")]
    public List<DiffWord>? NewWords { get; set; }
}

public class DiffWord
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("changed")]
    public bool Changed { get; set; }
}

public class DiffStats
{
    [JsonPropertyName("added")]
    public int Added { get; set; }

    [JsonPropertyName("deleted")]
    public int Deleted { get; set; }

    [JsonPropertyName("modified")]
    public int Modified { get; set; }
}
