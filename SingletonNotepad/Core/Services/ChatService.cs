using SingletonNotepad.Core.Models;
using SingletonNotepad.Core.Providers;
using SingletonNotepad.Core.Services;

namespace SingletonNotepad.Core.Services;

public interface IChatService
{
    Task<string> SendAsync(string userMessage, string? contextContent, CancellationToken ct = default);
}

public class ChatService : IChatService
{
    private readonly ILlmProviderSelector _providerSelector;
    private readonly ISettingsService _settingsService;

    public ChatService(ILlmProviderSelector providerSelector, ISettingsService settingsService)
    {
        _providerSelector = providerSelector;
        _settingsService = settingsService;
    }

    public async Task<string> SendAsync(string userMessage, string? contextContent, CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var providerName = !string.IsNullOrEmpty(settings.ChatLlmProvider)
            ? settings.ChatLlmProvider
            : settings.LlmProvider;
        var provider = _providerSelector.GetProvider(providerName) ?? _providerSelector.Current;

        var systemPrompt = BuildSystemPrompt(contextContent);

        return await provider.CompleteAsync(systemPrompt, userMessage, ct);
    }

    private static string BuildSystemPrompt(string? contextContent)
    {
        if (string.IsNullOrWhiteSpace(contextContent))
        {
            return "Tu es un assistant d'écriture. Aide l'utilisateur à améliorer, reformuler ou compléter son texte. Réponds en markdown.";
        }

        return $@"Tu es un assistant d'écriture. Voici le contexte actuel de la note de l'utilisateur :

---
{contextContent}
---

Aide l'utilisateur à améliorer, reformuler ou compléter ce texte. Réponds en markdown. Si l'utilisateur demande une modification, fournis le texte complet modifié.";
    }
}
