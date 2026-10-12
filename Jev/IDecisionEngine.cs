namespace AgentFrameworkJevSample.Jev;

/// <summary>
/// A typed decision engine, modeled on TypeSafeAI's JEV "System One" API
/// (https://docs.typesafe.ai). Given a piece of state (free text) and a set of
/// labeled criteria, it returns a structured choice + confidence instead of
/// free-form text that would need to be parsed.
/// </summary>
public interface IDecisionEngine
{
    /// <summary>
    /// Asks a single "Choice" question: which of the given <paramref name="criteria"/>
    /// (key = option name, value = description of when to pick it) best applies to
    /// <paramref name="state"/>.
    /// </summary>
    Task<ChoiceDecision> ChooseAsync(
        string state,
        string instructions,
        IReadOnlyDictionary<string, string> criteria,
        CancellationToken cancellationToken = default);
}
