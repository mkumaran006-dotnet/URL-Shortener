using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Parses raw requirements, identifies ambiguities, and produces a normalized specification.
/// This is the entry point for all three scenario types.
/// </summary>
public class RequirementsStage : IStage
{
    public StageName Name => StageName.Requirements;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var rawReq = ctx.Input.GetValueOrDefault("requirements")?.ToString()
                     ?? "Build a URL shortener with analytics and reliability features.";

        var spec = NormalizeRequirements(rawReq, ctx.ScenarioType);

        result.Outputs["normalized_spec"] = spec.NormalizedText;
        result.Outputs["ambiguities"]     = spec.Ambiguities;
        result.Outputs["constraints"]     = spec.Constraints;
        result.Outputs["acceptance_criteria"] = spec.AcceptanceCriteria;
        result.Artifacts.Add("requirements-spec.md");

        ctx.RecordDecision(
            Name.ToString(),
            $"Adopted {spec.Ambiguities.Count} clarification notes for scenario {ctx.ScenarioType}",
            "Requirements contained ambiguous terms that were resolved through heuristics and defaults.",
            "requirements-agent",
            new Dictionary<string, object?> { ["ambiguity_count"] = spec.Ambiguities.Count });

        return Task.CompletedTask;
    }

    private static NormalizedSpec NormalizeRequirements(string raw, ScenarioType scenario)
    {
        var ambiguities = new List<string>();
        var constraints = new List<string>
        {
            "Short codes must be URL-safe (alphanumeric + _ and -).",
            "Redirects must complete within 50 ms P99.",
            "Analytics data retention: 90 days."
        };

        var acceptance = new List<string>
        {
            "POST /api/urls returns 201 with a short code.",
            "GET /{code} redirects with HTTP 301.",
            "GET /api/urls/{code}/analytics returns click counts.",
            "Expired URLs return HTTP 410.",
            "Rate limit: 100 requests/minute per IP."
        };

        switch (scenario)
        {
            case ScenarioType.Greenfield:
                ambiguities.Add("Storage backend not specified — defaulting to SQLite for local dev, PostgreSQL for production.");
                ambiguities.Add("Authentication not required for MVP; add API-key auth in v2.");
                break;

            case ScenarioType.Brownfield:
                ambiguities.Add("Existing system scope unknown — assuming in-process enhancement, no migration needed.");
                constraints.Add("Backward compatibility: existing short codes must continue to resolve.");
                acceptance.Add("Existing short codes resolve without modification.");
                break;

            case ScenarioType.Ambiguous:
                ambiguities.Add("'Make it faster' interpreted as: cache hot URLs in-memory, add DB indexes, enable HTTP/2.");
                ambiguities.Add("'More reliable' interpreted as: health check endpoint, retry on DB transient errors, rate limiting.");
                acceptance.Add("Health check endpoint returns 200 within 200 ms.");
                break;
        }

        return new NormalizedSpec(raw, ambiguities, constraints, acceptance);
    }
}

internal record NormalizedSpec(
    string NormalizedText,
    List<string> Ambiguities,
    List<string> Constraints,
    List<string> AcceptanceCriteria);
