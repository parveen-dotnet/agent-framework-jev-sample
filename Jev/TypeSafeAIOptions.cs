namespace AgentFrameworkJevSample.Jev;

/// <summary>
/// Strongly-typed options bound from the "TypeSafeAI" configuration section
/// (appsettings.json / appsettings.Development.json / user-secrets), consumed via
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
/// </summary>
public sealed class TypeSafeAIOptions
{
    public const string SectionName = "TypeSafeAI";

    public string? ApiKey { get; set; }

    public string Endpoint { get; set; } = "https://api.typesafe.ai/v1/systemone";
}
