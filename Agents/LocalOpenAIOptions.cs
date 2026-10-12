namespace AgentFrameworkJevSample.Agents;

/// <summary>
/// Strongly-typed options bound from the "LocalOpenAI" configuration section
/// (appsettings.json / appsettings.Development.json / user-secrets), consumed via
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
///
/// Use this instead of <see cref="AzureAIFoundryOptions"/> when pointing at a local,
/// OpenAI-compatible inference server such as LM Studio
/// (e.g. http://127.0.0.1:1234) or Ollama's OpenAI-compatible endpoint. These
/// servers are plain HTTP (no TLS) and don't use Azure AD/Bearer auth, so they use
/// the OpenAI SDK's API-key-credential auth instead of DefaultAzureCredential.
/// </summary>
public sealed class LocalOpenAIOptions
{
    public const string SectionName = "LocalOpenAI";

    /// <summary>
    /// Base URL of the local OpenAI-compatible server, e.g. "http://127.0.0.1:1234/v1" for LM Studio.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Model id as exposed by the local server, e.g. "qwen/qwen3-1.7b" for LM Studio.
    /// </summary>
    public string ModelId { get; set; } = "local-model";

    /// <summary>
    /// Most local servers (LM Studio, Ollama, etc.) don't validate the API key, but the
    /// OpenAI SDK requires a non-empty value to be supplied. Any placeholder string works.
    /// </summary>
    public string ApiKey { get; set; } = "lm-studio";
}
