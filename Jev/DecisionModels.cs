using System.Text.Json.Serialization;

namespace AgentFrameworkJevSample.Jev;

/// <summary>
/// Result of a JEV "Choice" decision: which option was picked, how confident the
/// model is, and the full probability distribution over all offered options.
/// </summary>
public sealed record ChoiceDecision(string Choice, double Confidence, IReadOnlyDictionary<string, double> Probabilities);

// ---- Request DTOs (shape defined by https://docs.typesafe.ai) ----

internal sealed class SystemOneRequest
{
    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("model")]
    public string Model { get; init; } = "jev-latest";

    [JsonPropertyName("questions")]
    public required Dictionary<string, ChoiceQuestion> Questions { get; init; }
}

internal sealed class ChoiceQuestion
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "choice";

    [JsonPropertyName("instructions")]
    public required string Instructions { get; init; }

    [JsonPropertyName("criteria")]
    public required Dictionary<string, string> Criteria { get; init; }
}

// ---- Response DTOs ----

internal sealed class SystemOneResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("answers")]
    public Dictionary<string, ChoiceAnswer>? Answers { get; init; }
}

internal sealed class ChoiceAnswer
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("choice")]
    public string? Choice { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("probabilities")]
    public Dictionary<string, double>? Probabilities { get; init; }
}
