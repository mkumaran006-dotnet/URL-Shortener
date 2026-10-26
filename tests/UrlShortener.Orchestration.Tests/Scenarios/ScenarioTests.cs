using Microsoft.Extensions.Logging.Abstractions;
using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Governance;
using UrlShortener.Orchestration.Models;
using UrlShortener.Orchestration.Reliability;
using UrlShortener.Orchestration.Scenarios;
using UrlShortener.Orchestration.Stages;

namespace UrlShortener.Orchestration.Tests.Scenarios;

public class ScenarioTests
{
    private static IOrchestrationEngine BuildEngine()
    {
        var auditLogger = new AuditLogger(NullLogger<AuditLogger>.Instance);
        var approvalSvc = new AutoApprovalService(NullLogger<AutoApprovalService>.Instance, TimeSpan.Zero);
        var guardrails  = PolicyGuardrailService.CreateDefault();
        var rollback    = new RollbackManager(auditLogger, NullLogger<RollbackManager>.Instance);
        var metrics     = new MetricsCollector();
        IEnumerable<IStage> stages =
        [
            new RequirementsStage(),
            new ArchitectureStage(),
            new ImplementationStage(),
            new TestingStage(),
            new DocumentationStage(),
            new ReleaseReadinessStage()
        ];
        return new OrchestrationEngine(stages, auditLogger, approvalSvc, guardrails, rollback, metrics,
            NullLogger<OrchestrationEngine>.Instance);
    }

    [Fact]
    public async Task Greenfield_RequirementsStage_ResolvesAmbiguities()
    {
        var scenario = new GreenfieldScenario(BuildEngine());
        var ctx = await scenario.RunAsync();

        var ambiguities = ctx.GetOutput<List<string>>(StageName.Requirements, "ambiguities");
        Assert.NotNull(ambiguities);
        Assert.True(ambiguities.Count > 0);
    }

    [Fact]
    public async Task Brownfield_ImpactedModulesAreRecorded()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Brownfield, new Dictionary<string, object?>
        {
            ["impacted_modules"] = new[] { "UrlsController", "ShortUrl entity" }
        });

        Assert.Equal(WorkflowStatus.Completed, ctx.Status);
        // Constraints recorded by requirements stage include backward-compat rule
        var constraints = ctx.GetOutput<List<string>>(StageName.Requirements, "constraints");
        Assert.Contains(constraints!, c => c.Contains("Backward compatibility"));
    }

    [Fact]
    public async Task Ambiguous_RequirementsStage_ClarificationsNoted()
    {
        var scenario = new AmbiguousScenario(BuildEngine());
        var ctx = await scenario.RunAsync();

        var ambiguities = ctx.GetOutput<List<string>>(StageName.Requirements, "ambiguities");
        Assert.NotNull(ambiguities);
        // Ambiguous scenario produces at least 2 clarifications (faster + more reliable)
        Assert.True(ambiguities.Count >= 2);
    }

    [Fact]
    public async Task AllScenarios_ProduceReleaseCheckArtifact()
    {
        var scenarios = new ScenarioType[] { ScenarioType.Greenfield, ScenarioType.Brownfield, ScenarioType.Ambiguous };

        foreach (var s in scenarios)
        {
            var ctx = await BuildEngine().RunAsync(s);
            var releaseArtifacts = ctx.StageResults[StageName.ReleaseReadiness].Artifacts;
            Assert.Contains(releaseArtifacts, a => a.Contains("release-checklist"));
        }
    }
}
