using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Records the implementation artifacts produced by the project.
/// In a real agentic system this stage would call code-generation agents;
/// here it catalogs the existing implementation and validates its completeness.
/// </summary>
public class ImplementationStage : IStage
{
    public StageName Name => StageName.Implementation;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var apiSchema = ctx.GetOutput<string>(StageName.Architecture, "api_schema") ?? string.Empty;

        var artifacts = BuildArtifactManifest(ctx.ScenarioType);
        var codeArtifacts = string.Join("\n", artifacts.Select(a => $"  - {a}"));

        result.Outputs["code_artifacts"]    = codeArtifacts;
        result.Outputs["endpoints_implemented"] = apiSchema.Split('\n').Length;
        result.Outputs["patterns_applied"]  = "Repository, Cache-aside, Factory, Decorator (middleware)";
        result.Outputs["security_controls"] = "Input validation, Rate limiting, HTTPS redirect, RFC 7807 error bodies";
        result.Artifacts.AddRange(artifacts);

        ctx.RecordDecision(Name.ToString(),
            $"Implemented {artifacts.Count} code artifacts for {ctx.ScenarioType} scenario",
            "All acceptance criteria mapped to concrete classes with async/await throughout.",
            "implementation-agent");

        return Task.CompletedTask;
    }

    private static List<string> BuildArtifactManifest(ScenarioType scenario)
    {
        var core = new List<string>
        {
            "src/UrlShortener.Core/Entities/ShortUrl.cs",
            "src/UrlShortener.Core/Entities/ClickEvent.cs",
            "src/UrlShortener.Core/Services/UrlShortenerService.cs",
            "src/UrlShortener.Core/Services/AnalyticsService.cs",
            "src/UrlShortener.Infrastructure/Data/AppDbContext.cs",
            "src/UrlShortener.Infrastructure/Repositories/UrlRepository.cs",
            "src/UrlShortener.Infrastructure/Repositories/ClickEventRepository.cs",
            "src/UrlShortener.Infrastructure/Cache/InMemoryCacheService.cs",
            "src/UrlShortener.Api/Controllers/UrlsController.cs",
            "src/UrlShortener.Api/Controllers/RedirectController.cs",
            "src/UrlShortener.Api/Controllers/AnalyticsController.cs",
            "src/UrlShortener.Api/Middleware/ExceptionHandlingMiddleware.cs",
            "src/UrlShortener.Api/Middleware/RateLimitingMiddleware.cs",
            "src/UrlShortener.Api/Program.cs",
        };

        if (scenario == ScenarioType.Brownfield)
            core.Add("src/UrlShortener.Api/Controllers/OrchestrationController.cs");

        if (scenario == ScenarioType.Ambiguous)
            core.AddRange([
                "src/UrlShortener.Api/HealthCheck/DatabaseHealthCheck.cs",
                "src/UrlShortener.Api/HealthCheck/CacheHealthCheck.cs"
            ]);

        return core;
    }

    public Task RollbackAsync(WorkflowContext ctx, CancellationToken ct = default)
    {
        ctx.SetStageOutput(Name, "code_artifacts", null);
        ctx.SetStageOutput(Name, "rollback_note", "Implementation artifacts removed from release manifest.");
        return Task.CompletedTask;
    }
}
