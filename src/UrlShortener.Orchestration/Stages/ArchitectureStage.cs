using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Produces architecture decision records (ADRs), component diagram, and API schema.
/// Requires human approval before Implementation may proceed.
/// </summary>
public class ArchitectureStage : IStage
{
    public StageName Name => StageName.Architecture;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var spec = ctx.GetOutput<string>(StageName.Requirements, "normalized_spec") ?? string.Empty;
        var scenario = ctx.ScenarioType;

        var adrs = BuildAdrs(scenario);
        var components = BuildComponentDiagram(scenario);
        var apiSchema = BuildApiSchema();

        result.Outputs["adrs"]          = adrs;
        result.Outputs["components"]    = components;
        result.Outputs["api_schema"]    = apiSchema;
        result.Outputs["tech_stack"]    = "ASP.NET Core 8, EF Core + SQLite, In-Memory Cache, xUnit";
        result.Artifacts.AddRange(["architecture-adrs.md", "component-diagram.md", "openapi-schema.yaml"]);

        foreach (var adr in adrs)
            ctx.RecordDecision(Name.ToString(), adr.Title, adr.Rationale, "architecture-agent",
                new Dictionary<string, object?> { ["status"] = adr.Status });

        return Task.CompletedTask;
    }

    private static List<Adr> BuildAdrs(ScenarioType scenario) =>
    [
        new("ADR-001", "Use Base62 encoding for short codes",
            "Provides URL-safe, dense encoding with ~56B combos per 6 chars. Avoids ambiguous characters (0/O, 1/l).",
            "Accepted"),
        new("ADR-002", "In-process in-memory cache for hot URLs",
            "Avoids Redis dependency for MVP; reduces DB round-trips for frequently accessed URLs. Drawback: cache is not shared across instances.",
            scenario == ScenarioType.Ambiguous ? "Accepted — directly addresses performance requirement" : "Accepted"),
        new("ADR-003", "Async click recording (fire-and-forget)",
            "Redirect latency must be <50 ms. Synchronous click recording would add ~5-15 ms. Trade-off: click count may lag by one record under failure.",
            "Accepted"),
        new("ADR-004", "Clean architecture layering (Core / Infrastructure / API)",
            "Enforces dependency inversion: API and Infrastructure both depend on Core abstractions. Enables swapping SQLite → PostgreSQL with zero Core changes.",
            "Accepted"),
        new("ADR-005", "SQLite for development, abstracted behind EF Core",
            "Zero-config local dev experience. Production deployment swaps connection string only.",
            "Accepted"),
    ];

    private static string BuildComponentDiagram(ScenarioType scenario) =>
        $"""
        Components ({scenario} scenario)
        ─────────────────────────────────
        [Client] → [ASP.NET Core API]
                         │
              ┌──────────┼───────────────┐
              ▼          ▼               ▼
        [UrlsController] [RedirectController] [AnalyticsController]
              │                │
              ▼                ▼
        [UrlShortenerService] [AnalyticsService]
              │
        [IUrlRepository] ──► [EF Core + SQLite]
        [IClickEventRepository]
        [ICacheService]  ──► [IMemoryCache]
        """;

    private static string BuildApiSchema() =>
        """
        POST   /api/urls                         → 201 UrlResponse
        GET    /api/urls                          → 200 PagedResponse<UrlResponse>
        GET    /api/urls/{code}                   → 200 UrlResponse
        PATCH  /api/urls/{code}                   → 200 UrlResponse
        DELETE /api/urls/{code}                   → 204
        GET    /{code}                            → 301 redirect
        GET    /api/urls/{code}/analytics         → 200 AnalyticsSummaryResponse
        GET    /api/urls/{code}/analytics/daily   → 200 DailyClickResponse[]
        GET    /api/urls/{code}/analytics/referrers → 200 ReferrerResponse[]
        GET    /api/urls/{code}/analytics/countries → 200 CountryResponse[]
        GET    /health                            → 200
        """;
}

internal record Adr(string Id, string Title, string Rationale, string Status);
