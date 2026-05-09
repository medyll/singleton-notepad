using System.Text;
using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Services;

public interface IChatService
{
    Task<(string Response, List<string> UsedSkills)> SendAsync(string userMessage, string? contextContent, CancellationToken ct = default);
}

public class ChatService : IChatService
{
    private readonly ILlmProviderSelector _providerSelector;
    private readonly ISettingsService _settingsService;
    private readonly ISkillService _skillService;

    public ChatService(ILlmProviderSelector providerSelector, ISettingsService settingsService, ISkillService skillService)
    {
        _providerSelector = providerSelector;
        _settingsService = settingsService;
        _skillService = skillService;
    }

    public async Task<(string Response, List<string> UsedSkills)> SendAsync(string userMessage, string? contextContent, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var providerName = !string.IsNullOrEmpty(settings.ChatLlmProvider)
            ? settings.ChatLlmProvider
            : settings.LlmProvider;
        var provider = _providerSelector.GetProvider(providerName) ?? _providerSelector.Current;

        // Parse manual /skill-name invocation
        var (cleanedMessage, forcedSkill, skillNotFound) = ParseSkillInvocation(userMessage);

        if (skillNotFound != null)
        {
            return ($"⚠ Skill \"{skillNotFound}\" introuvable", []);
        }

        // Auto-select skills based on context
        var autoSkills = _skillService.SelectForContext(cleanedMessage, contextContent);

        // Merge: forced skill first (if not already present), then auto-selected
        var allSkills = new List<SkillDefinition>();
        if (forcedSkill != null)
            allSkills.Add(forcedSkill);
        foreach (var s in autoSkills)
        {
            if (!allSkills.Any(x => string.Equals(x.Name, s.Name, StringComparison.OrdinalIgnoreCase)))
                allSkills.Add(s);
        }

        var systemPrompt = BuildSystemPrompt(contextContent, allSkills);
        var response = await provider.CompleteAsync(systemPrompt, cleanedMessage, ct);
        var usedSkillNames = allSkills.Select(s => s.Name).ToList();

        return (response, usedSkillNames);
    }

    private (string cleanedMessage, SkillDefinition? forced, string? notFound) ParseSkillInvocation(string userMessage)
    {
        if (!userMessage.StartsWith('/'))
            return (userMessage, null, null);

        var spaceIdx = userMessage.IndexOf(' ');
        string skillName;
        string rest;

        if (spaceIdx < 0)
        {
            skillName = userMessage[1..];
            rest = string.Empty;
        }
        else
        {
            skillName = userMessage[1..spaceIdx];
            rest = userMessage[(spaceIdx + 1)..];
        }

        if (string.IsNullOrWhiteSpace(skillName))
            return (userMessage, null, null);

        var skill = _skillService.GetByName(skillName);
        if (skill == null)
            return (rest, null, skillName);

        return (rest, skill, null);
    }

    private static string BuildSystemPrompt(string? contextContent, IReadOnlyList<SkillDefinition> skills)
    {
        var sb = new StringBuilder();

        SkillService.AppendSkillsBlock(sb, skills);

        if (!string.IsNullOrWhiteSpace(contextContent))
        {
            sb.AppendLine("[CONTEXTE DOCUMENT]");
            sb.AppendLine("<contenu_note>");
            sb.AppendLine(EscapeXmlTags(contextContent));
            sb.AppendLine("</contenu_note>");
            sb.AppendLine();
        }

        sb.AppendLine("[INSTRUCTIONS]");
        sb.AppendLine("Tu es un assistant d'écriture. Aide l'utilisateur à améliorer, reformuler ou compléter son texte. Réponds en markdown. Si l'utilisateur demande une modification, fournis le texte complet modifié.");

        return sb.ToString();
    }

    private static string EscapeXmlTags(string content)
    {
        return content.Replace("</contenu_note>", "&lt;/contenu_note&gt;", StringComparison.OrdinalIgnoreCase)
                      .Replace("<contenu_note>", "&lt;contenu_note&gt;", StringComparison.OrdinalIgnoreCase);
    }
}
