using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Scenarios;

/// <summary>
/// Brownfield: enhance an existing URL shortener with analytics and rate limiting.
/// The orchestration engine identifies impacted modules and constrains the
/// implementation to preserve backward compatibility.
/// </summary>
public class BrownfieldScenario(IOrchestrationEngine engine)
{
    public Task<WorkflowContext> RunAsync(CancellationToken ct = default) =>
        engine.RunAsync(ScenarioType.Brownfield, new Dictionary<string, object?>
        {
            ["requirements"] = """
                Enhance the existing URL shortener:
                1. Add click analytics API (GET /api/urls/{code}/analytics)
                2. Add per-IP rate limiting (100 req/min)
                3. Add URL expiry support (ExpiresAt field)
                CONSTRAINT: Existing short codes must continue to resolve unchanged.
                CONSTRAINT: No breaking changes to existing API contract.
                """,
            ["existing_modules"] = new[]
            {
                "UrlsController (POST /api/urls, GET /{code})",
                "ShortUrl entity (Id, ShortCode, OriginalUrl, CreatedAt)",
                "SqliteRepository (basic CRUD)"
            },
            ["impacted_modules"] = new[]
            {
                "ShortUrl entity — add ClickCount, ExpiresAt, IsActive fields",
                "UrlRepository — add IncrementClickCountAsync, update queries",
                "Program.cs — register rate limiting middleware",
                "New: ClickEvent entity + ClickEventRepository",
                "New: AnalyticsController + AnalyticsService"
            }
        }, ct);
}
