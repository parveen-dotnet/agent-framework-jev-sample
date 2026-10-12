using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AgentFrameworkJevSample.Jev;

/// <summary>
/// Real HTTP client for the JEV (TypeSafeAI) System One API:
/// POST https://api.typesafe.ai/v1/systemone
/// Docs: https://docs.typesafe.ai/introduction/quickstart
/// Configuration (ApiKey/Endpoint) is supplied via <see cref="IOptions{TOptions}"/>
/// of <see cref="TypeSafeAIOptions"/>, bound from appsettings.json.
/// </summary>
public sealed class JevDecisionEngine : IDecisionEngine
{
    private const string QuestionKey = "assignee";

    private readonly HttpClient _httpClient;

    public JevDecisionEngine(HttpClient httpClient, IOptions<TypeSafeAIOptions> options)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "A TypeSafeAI (JEV) API key is required. Set 'TypeSafeAI:ApiKey' in appsettings.Development.json or user-secrets.",
                nameof(options));
        }

        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri(options.Value.Endpoint);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
    }

    public async Task<ChoiceDecision> ChooseAsync(
        string state,
        string instructions,
        IReadOnlyDictionary<string, string> criteria,
        CancellationToken cancellationToken = default)
    {
        var request = new SystemOneRequest
        {
            State = state,
            Model = "jev-latest",
            Questions = new Dictionary<string, ChoiceQuestion>
            {
                [QuestionKey] = new ChoiceQuestion
                {
                    Instructions = instructions,
                    Criteria = criteria.ToDictionary(kv => kv.Key, kv => kv.Value),
                },
            },
        };

        using var httpResponse = await _httpClient
            .PostAsJsonAsync(string.Empty, request, cancellationToken)
            .ConfigureAwait(false);

        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content
            .ReadFromJsonAsync<SystemOneResponse>(cancellationToken)
            .ConfigureAwait(false);

        if (response?.Answers is null || !response.Answers.TryGetValue(QuestionKey, out var answer) || answer.Choice is null)
        {
            throw new InvalidOperationException("JEV response did not contain the expected 'assignee' choice answer.");
        }

        return new ChoiceDecision(
            answer.Choice,
            answer.Confidence,
            answer.Probabilities ?? new Dictionary<string, double>());
    }
}
