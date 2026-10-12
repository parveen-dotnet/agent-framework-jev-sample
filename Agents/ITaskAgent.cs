namespace AgentFrameworkJevSample.Agents;

/// <summary>
/// Minimal abstraction over "an agent that can handle a task and return text",
/// so the Manager/Orchestrator can work identically whether the underlying
/// implementation is a real Microsoft Agent Framework AIAgent (Azure AI Foundry)
/// or the offline mock fallback.
/// </summary>
public interface ITaskAgent
{
    string Name { get; }

    Task<string> HandleAsync(string task, CancellationToken cancellationToken = default);
}
