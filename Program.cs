using AgentFrameworkJevSample.Agents;
using AgentFrameworkJevSample.Jev;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

Console.WriteLine("=== Welcome to learning with Parveen Sharma ===");
Console.WriteLine("=== Manager/Orchestrator + Dev/QA Agent Framework Sample ===");
Console.WriteLine("Routing decisions are made by JEV (TypeSafeAI); Dev/QA agents run on a local OpenAI-compatible model (e.g. LM Studio) or Azure AI Foundry.\n");

// ---- Configuration: appsettings.json (+ optional appsettings.Development.json / user-secrets overrides) ----
var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .Build();

// ---- DI: bind strongly-typed options via IOptions<T>, per Microsoft.Extensions.Options conventions ----
var services = new ServiceCollection();
services.AddHttpClient();
services.Configure<TypeSafeAIOptions>(configuration.GetSection(TypeSafeAIOptions.SectionName));
services.Configure<AzureAIFoundryOptions>(configuration.GetSection(AzureAIFoundryOptions.SectionName));
services.Configure<LocalOpenAIOptions>(configuration.GetSection(LocalOpenAIOptions.SectionName));
using var serviceProvider = services.BuildServiceProvider();

var typeSafeOptions = serviceProvider.GetRequiredService<IOptions<TypeSafeAIOptions>>();
var foundryOptions = serviceProvider.GetRequiredService<IOptions<AzureAIFoundryOptions>>();
var localOpenAiOptions = serviceProvider.GetRequiredService<IOptions<LocalOpenAIOptions>>();
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

// ---- JEV (TypeSafeAI) decision engine ----
IDecisionEngine decisionEngine;
if (!string.IsNullOrWhiteSpace(typeSafeOptions.Value.ApiKey))
{
    decisionEngine = new JevDecisionEngine(httpClientFactory.CreateClient(), typeSafeOptions);
    Console.WriteLine("[Jev] Using real TypeSafeAI System One API.");
}
else
{
    decisionEngine = new MockDecisionEngine();
    Console.WriteLine("[Jev] TypeSafeAI:ApiKey not set in appsettings - using MockDecisionEngine (keyword-based) fallback.");
}

// ---- Dev / QA agents: prefer a local OpenAI-compatible server (e.g. LM Studio) when configured,
// then Azure AI Foundry, then fall back to mocks. ----
ITaskAgent devAgent;
ITaskAgent qaAgent;
if (!string.IsNullOrWhiteSpace(localOpenAiOptions.Value.Endpoint))
{
    (devAgent, qaAgent) = FoundryAgentFactory.CreateLocalDevAndQaAgents(localOpenAiOptions);
    Console.WriteLine($"[Agents] Using local OpenAI-compatible model '{localOpenAiOptions.Value.ModelId}' at {localOpenAiOptions.Value.Endpoint}.");
}
else if (!string.IsNullOrWhiteSpace(foundryOptions.Value.Endpoint))
{
    (devAgent, qaAgent) = FoundryAgentFactory.CreateDevAndQaAgents(foundryOptions);
    Console.WriteLine($"[Agents] Using Azure AI Foundry deployment '{foundryOptions.Value.ModelDeployment}' at {foundryOptions.Value.Endpoint}.");
}
else
{
    devAgent = new MockTaskAgent(FoundryAgentFactory.DevAgentName, "senior developer");
    qaAgent = new MockTaskAgent(FoundryAgentFactory.QaAgentName, "QA engineer");
    Console.WriteLine("[Agents] Neither LocalOpenAI:Endpoint nor AzureAIFoundry:Endpoint set in appsettings - using MockTaskAgent fallback for Dev/QA.");
}

var manager = new ManagerOrchestrator(decisionEngine, devAgent, qaAgent);

Console.WriteLine();
Console.WriteLine("Enter a task for the Manager to route (or 'exit' to quit):");

while (true)
{
    Console.Write("\n> ");
    var task = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(task) || task.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var result = await manager.HandleTaskAsync(task);

        Console.WriteLine($"\n[Manager/JEV] Assigned to: {result.AssignedAgentName} " +
                           $"(choice='{result.Choice}', confidence={result.Confidence:P0})");
        Console.WriteLine("[Manager/JEV] Probabilities: " +
                           string.Join(", ", result.Probabilities.Select(kv => $"{kv.Key}={kv.Value:P0}")));
        Console.WriteLine($"\n[{result.AssignedAgentName}] {result.Response}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling task: {ex.Message}");
    }
}

Console.WriteLine("\nGoodbye.");
