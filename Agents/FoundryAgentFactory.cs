using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AgentFrameworkJevSample.Agents;

/// <summary>
/// Creates the Dev and QA agents, either backed by Azure AI Foundry model
/// deployments (<see cref="CreateDevAndQaAgents(IOptions{AzureAIFoundryOptions})"/>,
/// auth via Azure AD / Bearer) or by a local OpenAI-compatible server such as LM
/// Studio (<see cref="CreateLocalDevAndQaAgents(IOptions{LocalOpenAIOptions})"/>,
/// auth via a plain API-key credential - no Bearer/TLS requirement), following the
/// Microsoft Agent Framework "AsAIAgent" pattern:
/// https://learn.microsoft.com/en-us/agent-framework/get-started/your-first-agent
/// </summary>
public static class FoundryAgentFactory
{
    public const string DevAgentName = "DevAgent";
    public const string QaAgentName = "QaAgent";

    private const string DevInstructions =
        "You are a senior software developer. Implement or explain how to implement the requested " +
        "feature/fix concisely, including any relevant code snippets. Focus on correctness and clarity.";

    private const string QaInstructions =
        "You are a QA engineer. Given the requested task, describe how you would test/verify it: " +
        "test cases, edge cases, and how you'd confirm it works. Do not write production code, only test guidance.";

    /// <summary>
    /// Builds both the Dev and QA agents from Azure AI Foundry options (project endpoint + model deployment).
    /// Uses <see cref="DefaultAzureCredential"/> (Azure AD Bearer token) - requires an HTTPS endpoint.
    /// </summary>
    public static (ITaskAgent Dev, ITaskAgent Qa) CreateDevAndQaAgents(IOptions<AzureAIFoundryOptions> options)
    {
        var foundryOptions = options.Value;
        if (string.IsNullOrWhiteSpace(foundryOptions.Endpoint))
        {
            throw new ArgumentException(
                "An Azure AI Foundry project endpoint is required. Set 'AzureAIFoundry:Endpoint' in appsettings.Development.json or user-secrets.",
                nameof(options));
        }

        var projectClient = new AIProjectClient(new Uri(foundryOptions.Endpoint), new DefaultAzureCredential());

        var devAgent = projectClient.AsAIAgent(
            model: foundryOptions.ModelDeployment,
            instructions: DevInstructions,
            name: DevAgentName);

        var qaAgent = projectClient.AsAIAgent(
            model: foundryOptions.ModelDeployment,
            instructions: QaInstructions,
            name: QaAgentName);

        return (new AIAgentAdapter(devAgent), new AIAgentAdapter(qaAgent));
    }

    /// <summary>
    /// Builds both the Dev and QA agents from a local OpenAI-compatible server (e.g. LM Studio,
    /// Ollama). Uses a plain API-key credential rather than Bearer/Azure AD, so it works fine
    /// over plain HTTP (no TLS) - unlike <see cref="DefaultAzureCredential"/>, which refuses to
    /// attach a Bearer token to a non-HTTPS endpoint.
    /// </summary>
    public static (ITaskAgent Dev, ITaskAgent Qa) CreateLocalDevAndQaAgents(IOptions<LocalOpenAIOptions> options)
    {
        var localOptions = options.Value;
        if (string.IsNullOrWhiteSpace(localOptions.Endpoint))
        {
            throw new ArgumentException(
                "A local OpenAI-compatible endpoint is required. Set 'LocalOpenAI:Endpoint' in appsettings.Development.json or user-secrets.",
                nameof(options));
        }

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(localOptions.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(localOptions.Endpoint) });

        var chatClient = openAiClient.GetChatClient(localOptions.ModelId);

        var devAgent = chatClient.AsAIAgent(instructions: DevInstructions, name: DevAgentName);
        var qaAgent = chatClient.AsAIAgent(instructions: QaInstructions, name: QaAgentName);

        return (new AIAgentAdapter(devAgent), new AIAgentAdapter(qaAgent));
    }

    /// <summary>
    /// Adapts a Microsoft Agent Framework <see cref="AIAgent"/> to the sample's
    /// lightweight <see cref="ITaskAgent"/> abstraction.
    /// </summary>
    private sealed class AIAgentAdapter(AIAgent agent) : ITaskAgent
    {
        public string Name => agent.Name ?? "Agent";

        public async Task<string> HandleAsync(string task, CancellationToken cancellationToken = default)
        {
            var response = await agent.RunAsync(task, cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.ToString() ?? string.Empty;
        }
    }
}
