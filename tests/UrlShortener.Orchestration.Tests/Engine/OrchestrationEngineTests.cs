using Microsoft.Extensions.Logging.Abstractions;
using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Governance;
using UrlShortener.Orchestration.Models;
using UrlShortener.Orchestration.Reliability;
using UrlShortener.Orchestration.Stages;

namespace UrlShortener.Orchestration.Tests.Engine;

public class OrchestrationEngineTests
{
    private static OrchestrationEngine BuildEngine()
    {
        var auditLogger = new AuditLogger(NullLogger<AuditLogger>.Instance);
        var approvalSvc = new AutoApprovalService(NullLogger<AutoApprovalService>.Instance, TimeSpan.Zero);
        var guardrails  = PolicyGuardrailService.CreateDefault();
        var rollback    = new RollbackManager(auditLogger, NullLogger<RollbackManager>.Instance);
        var metrics     = new MetricsCollector();

        var stages = new List<IStage>
        {
            new RequirementsStage(),
            new ArchitectureStage(),
            new ImplementationStage(),
            new TestingStage(),
            new DocumentationStage(),
            new ReleaseReadinessStage(),
        };

        return new OrchestrationEngine(
            stages, auditLogger, approvalSvc, guardrails, rollback, metrics,
            NullLogger<OrchestrationEngine>.Instance);
    }

    [Fact]
    public async Task Run_Greenfield_CompletesSuccessfully()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        Assert.Equal(WorkflowStatus.Completed, ctx.Status);
        Assert.All(ctx.StageResults.Values, r => Assert.Equal(StageStatus.Completed, r.Status));
    }

    [Fact]
    public async Task Run_Brownfield_CompletesSuccessfully()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Brownfield);

        Assert.Equal(WorkflowStatus.Completed, ctx.Status);
    }

    [Fact]
    public async Task Run_Ambiguous_CompletesSuccessfully()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Ambiguous);

        Assert.Equal(WorkflowStatus.Completed, ctx.Status);
    }

    [Fact]
    public async Task Run_ProducesAuditLog_WithAllStages()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        Assert.NotEmpty(ctx.AuditLog);
        Assert.Contains(ctx.AuditLog, e => e.Stage == "Requirements" && e.Action == "StageCompleted");
        Assert.Contains(ctx.AuditLog, e => e.Stage == "Architecture" && e.Action == "ApprovalGranted");
        Assert.Contains(ctx.AuditLog, e => e.Stage == "ReleaseReadiness" && e.Action == "StageCompleted");
    }

    [Fact]
    public async Task Run_RecordsDecisionLineage()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        Assert.NotEmpty(ctx.DecisionLineage);
        // Architecture stage records at least one ADR per decision
        var archDecisions = ctx.DecisionLineage.Where(d => d.Stage == "Architecture").ToList();
        Assert.True(archDecisions.Count >= 5); // 5 ADRs defined in ArchitectureStage
    }

    [Fact]
    public async Task Run_MetricsReflectAllStages()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        Assert.Equal(6, ctx.Metrics.CompletedStages);
        Assert.Equal(0, ctx.Metrics.FailedStages);
        Assert.True(ctx.Metrics.TotalDuration.TotalMilliseconds > 0);
        Assert.Equal(100.0, ctx.Metrics.SuccessRate, precision: 0);
    }

    [Fact]
    public async Task GetStatus_ReturnsContext_AfterRun()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        var retrieved = engine.GetStatus(ctx.WorkflowId);

        Assert.NotNull(retrieved);
        Assert.Equal(ctx.WorkflowId, retrieved.WorkflowId);
    }

    [Fact]
    public async Task Requirements_ProducesNormalizedSpec()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        var spec = ctx.GetOutput<string>(StageName.Requirements, "normalized_spec");
        Assert.NotNull(spec);
    }

    [Fact]
    public async Task Architecture_ProducesAdrs()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        var adrs = ctx.GetOutput<object>(StageName.Architecture, "adrs");
        Assert.NotNull(adrs);
    }

    [Fact]
    public async Task ReleaseReadiness_ConfirmsReadyToRelease()
    {
        var engine = BuildEngine();
        var ctx = await engine.RunAsync(ScenarioType.Greenfield);

        var readyRaw = ctx.StageResults[StageName.ReleaseReadiness].Outputs["release_ready"];
        Assert.True(readyRaw is bool b && b);
    }
}
