using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Scenarios;

/// <summary>
/// Greenfield: build the URL shortener service from scratch.
/// All stages execute; Architecture requires human approval before Implementation.
/// </summary>
public class GreenfieldScenario(IOrchestrationEngine engine)
{
    public Task<WorkflowContext> RunAsync(CancellationToken ct = default) =>
        engine.RunAsync(ScenarioType.Greenfield, new Dictionary<string, object?>
        {
            ["requirements"] = """
                Build a URL shortener service with:
                - Core REST API (create, resolve, list, update, delete)
                - Click analytics (total count, daily breakdown, top referrers, country breakdown)
                - Reliability features (rate limiting, caching, expiry)
                - Clean architecture with dependency injection
                - SQLite database (dev), PostgreSQL-ready (production)
                - Swagger/OpenAPI documentation
                - 80%+ test coverage
                """,
            ["team"]        = "Platform Engineering",
            ["target_date"] = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")
        }, ct);
}
