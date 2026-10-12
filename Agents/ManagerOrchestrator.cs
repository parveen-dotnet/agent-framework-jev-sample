using AgentFrameworkJevSample.Jev;

namespace AgentFrameworkJevSample.Agents;

public sealed record ManagerResult(string AssignedAgentName, string Choice, double Confidence,
    IReadOnlyDictionary<string, double> Probabilities, string Response);

/// <summary>
/// The combined Orchestrator + Manager. It owns the Dev/QA agents and uses the
/// JEV (TypeSafeAI) decision engine as a typed routing layer: instead of asking an
/// LLM "which agent should do this?" and parsing free text, it asks JEV a single
/// Choice question and gets back a structured, calibrated answer.
/// </summary>
public sealed class ManagerOrchestrator(IDecisionEngine decisionEngine, ITaskAgent devAgent, ITaskAgent qaAgent)
{
    private static readonly IReadOnlyDictionary<string, string> AssignmentCriteria = new Dictionary<string, string>
    {
        ["dev"] = "Implementation, coding, building features, fixing bugs, writing or changing code",
        ["qa"] = "Testing, verification, reviewing quality, validating bug fixes, writing test plans",
    };

    private const string AssignmentInstructions =
        "Decide which team should handle this task: 'dev' for implementation/coding work, " +
        "'qa' for testing/verification work.";

    public async Task<ManagerResult> HandleTaskAsync(string task, CancellationToken cancellationToken = default)
    {
        var decision = await decisionEngine
            .ChooseAsync(task, AssignmentInstructions, AssignmentCriteria, cancellationToken)
            .ConfigureAwait(false);

        var chosenAgent = decision.Choice.Equals("qa", StringComparison.OrdinalIgnoreCase) ? qaAgent : devAgent;

        var response = await chosenAgent.HandleAsync(task, cancellationToken).ConfigureAwait(false);

        return new ManagerResult(
            chosenAgent.Name,
            decision.Choice,
            decision.Confidence,
            decision.Probabilities,
            response);
    }
}
