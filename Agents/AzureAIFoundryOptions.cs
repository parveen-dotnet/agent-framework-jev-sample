namespace AgentFrameworkJevSample.Agents;

/// <summary>
/// Strongly-typed options bound from the "AzureAIFoundry" configuration section
/// (appsettings.json / appsettings.Development.json / user-secrets), consumed via
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
/// </summary>
public sealed class AzureAIFoundryOptions
{
    public const string SectionName = "AzureAIFoundry";

    public string? Endpoint { get; set; }

    public string ModelDeployment { get; set; } = "gpt-4o-mini";
}
