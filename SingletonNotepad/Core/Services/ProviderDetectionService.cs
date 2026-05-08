namespace SingletonNotepad.Core.Services;

public record DetectedProvider(string Name, string BaseUrl, string ApiKey, string DefaultModel);

public static class ProviderDetectionService
{
    private static readonly Dictionary<string, (string Name, string BaseUrl, string DefaultModel)> KnownEnvProviders =
        new()
        {
            ["MISTRAL_API_KEY"]    = ("Mistral",   "https://api.mistral.ai/v1",       "mistral-small-latest"),
            ["GROQ_API_KEY"]       = ("Groq",       "https://api.groq.com/openai/v1",  "llama3-8b-8192"),
            ["TOGETHER_API_KEY"]   = ("Together",   "https://api.together.xyz/v1",     "meta-llama/Llama-3-8b-chat-hf"),
            ["OPENROUTER_API_KEY"] = ("OpenRouter", "https://openrouter.ai/api/v1",    "openai/gpt-4o-mini"),
            ["COHERE_API_KEY"]     = ("Cohere",     "https://api.cohere.ai/compatibility/v1", "command-r-plus"),
        };

    public static string GetDefaultModel(string providerName)
    {
        foreach (var (_, (name, _, defaultModel)) in KnownEnvProviders)
            if (name == providerName) return defaultModel;
        return string.Empty;
    }

    public static List<DetectedProvider> Detect()
    {
        var results = new List<DetectedProvider>();
        foreach (var (envKey, (name, baseUrl, defaultModel)) in KnownEnvProviders)
        {
            var key = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(key))
                results.Add(new DetectedProvider(name, baseUrl, key, defaultModel));
        }
        return results;
    }
}
