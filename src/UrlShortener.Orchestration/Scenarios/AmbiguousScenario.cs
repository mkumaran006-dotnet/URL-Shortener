using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Scenarios;

/// <summary>
/// Ambiguous: "Make it faster and more reliable."
/// Demonstrates requirement clarification — the Requirements stage resolves the
/// ambiguity by applying standard performance and reliability heuristics,
/// records the interpretations as decision lineage, and proceeds with a
/// concrete engineering plan.
/// </summary>
public class AmbiguousScenario(IOrchestrationEngine engine)
{
    public Task<WorkflowContext> RunAsync(CancellationToken ct = default) =>
        engine.RunAsync(ScenarioType.Ambiguous, new Dictionary<string, object?>
        {
            ["requirements"] = "Make the URL shortener faster and more reliable.",
            ["clarification_notes"] = new[]
            {
                "'Faster' → cache hot URLs in-memory, add composite DB index on ShortCode, compress responses",
                "'More reliable' → health-check endpoint, transient retry on DB errors, graceful shutdown"
            },
            ["assumed_baseline"] = "Existing service with no caching, no health check, no retry logic"
        }, ct);
}
