namespace AgentFrameworkJevSample.Agents;

/// <summary>
/// Offline fallback agent used only when Azure AI Foundry isn't configured, so the
/// sample still runs end-to-end. Clearly NOT a substitute for a real model response.
/// </summary>
public sealed class MockTaskAgent(string name, string persona) : ITaskAgent
{
    public string Name { get; } = name;

    public Task<string> HandleAsync(string task, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            $"[MOCK - no Azure AI Foundry configured] As the {Name} ({persona}), " +
            $"here is a placeholder response for task: \"{task}\"");
    }
}
