namespace AgentFrameworkJevSample.Jev;

/// <summary>
/// Fallback decision engine used only when no real JEV (TypeSafeAI) API key is
/// configured, so the sample can still be explored end-to-end. Picks the first
/// criterion whose key/description keyword-matches the state text, defaulting to
/// the first offered option. Clearly NOT a replacement for the real JEV model.
/// </summary>
public sealed class MockDecisionEngine : IDecisionEngine
{
    public Task<ChoiceDecision> ChooseAsync(
        string state,
        string instructions,
        IReadOnlyDictionary<string, string> criteria,
        CancellationToken cancellationToken = default)
    {
        var stateWords = state.ToLowerInvariant().Split(
            [' ', '.', ',', '!', '?', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries);

        // Score each criterion by how many of its description words also appear in the state text.
        var scores = criteria.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .ToLowerInvariant()
                .Split([' ', ',', '/'], StringSplitOptions.RemoveEmptyEntries)
                .Count(word => stateWords.Contains(word.TrimEnd(','))));

        var bestScore = scores.Values.Max();
        var match = bestScore > 0
            ? scores.First(kv => kv.Value == bestScore).Key
            : criteria.Keys.First();

        var probabilities = criteria.Keys.ToDictionary(
            key => key,
            key => key == match ? 0.7 : 0.3 / Math.Max(1, criteria.Count - 1));

        return Task.FromResult(new ChoiceDecision(match, probabilities[match], probabilities));
    }
}
