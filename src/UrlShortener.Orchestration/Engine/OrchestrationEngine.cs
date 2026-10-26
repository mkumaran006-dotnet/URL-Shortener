using Microsoft.Extensions.Logging;
using UrlShortener.Orchestration.Gates;
using UrlShortener.Orchestration.Governance;
using UrlShortener.Orchestration.Models;
using UrlShortener.Orchestration.Reliability;
using UrlShortener.Orchestration.Stages;

namespace UrlShortener.Orchestration.Engine;

/// <summary>
/// Core agentic orchestration engine.
///
/// Execution model:
///   1. Build a dependency graph from stage definitions.
///   2. Validate the graph (no cycles, no missing deps).
///   3. Execute stages in topological batches — stages within a batch run in parallel.
///   4. For each stage: entry gate → policy guardrails → execute → exit gate → (approval if required).
///   5. On failure: bounded retry with exponential back-off → fallback → rollback if critical.
///   6. Capture full audit trail and decision lineage throughout.
///   7. If upstream output changes during execution, re-evaluate ready stages (dynamic re-planning).
/// </summary>
public class OrchestrationEngine(
    IEnumerable<IStage> stages,
    IAuditLogger auditLogger,
    IApprovalService approvalService,
    PolicyGuardrailService guardrails,
    RollbackManager rollback,
    MetricsCollector metrics,
    ILogger<OrchestrationEngine> logger) : IOrchestrationEngine
{
    private readonly Dictionary<StageName, IStage> _stages = stages.ToDictionary(s => s.Name);
    private readonly Dictionary<string, WorkflowContext> _workflows = new();
    private readonly Dictionary<string, CancellationTokenSource> _stopTokens = new();

    public async Task<WorkflowContext> RunAsync(
        ScenarioType scenario,
        Dictionary<string, object?>? input = null,
        CancellationToken ct = default)
    {
        var ctx = new WorkflowContext
        {
            ScenarioType = scenario,
            Input = input ?? []
        };
        ctx.Metrics.TotalStages = _stages.Count;

        _workflows[ctx.WorkflowId] = ctx;
        var stopSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _stopTokens[ctx.WorkflowId] = stopSource;

        auditLogger.Log(ctx, "engine", "WorkflowStarted", "orchestrator",
            $"Workflow {ctx.WorkflowId} started for scenario {scenario}.", Severity.Info);

        var graph = BuildGraph(scenario);
        if (!graph.Validate(out var errors))
        {
            ctx.Status = WorkflowStatus.Failed;
            auditLogger.Log(ctx, "engine", "GraphValidationFailed", "orchestrator",
                string.Join("; ", errors), Severity.Critical);
            return ctx;
        }

        // Register rollback handlers
        foreach (var stage in _stages.Values)
            rollback.Register(stage.Name, async (c) => await stage.RollbackAsync(c, stopSource.Token));

        ctx.Status = WorkflowStatus.Running;

        try
        {
            await ExecuteBatchesAsync(ctx, graph, stopSource.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            ctx.Status = WorkflowStatus.SafeStopped;
            auditLogger.Log(ctx, "engine", "SafeStopped", "operator",
                "Workflow was gracefully stopped by operator.", Severity.Warning);
        }
        catch (Exception ex)
        {
            ctx.Status = WorkflowStatus.Failed;
            auditLogger.Log(ctx, "engine", "WorkflowFailed", "orchestrator",
                $"Unrecoverable failure: {ex.Message}", Severity.Critical);
            await rollback.RollbackFromAsync(ctx, StageName.Requirements);
            throw;
        }
        finally
        {
            metrics.FinalizeWorkflow(ctx);
            _stopTokens.Remove(ctx.WorkflowId);
        }

        if (ctx.Status == WorkflowStatus.Running)
        {
            ctx.Status = ctx.StageResults.Values.All(r => r.Status == StageStatus.Completed)
                ? WorkflowStatus.Completed
                : WorkflowStatus.Failed;
        }

        auditLogger.Log(ctx, "engine", "WorkflowFinished", "orchestrator",
            $"Workflow {ctx.WorkflowId} finished with status {ctx.Status}. " +
            $"Duration={ctx.Metrics.TotalDuration.TotalSeconds:F1}s " +
            $"SuccessRate={ctx.Metrics.SuccessRate:F0}%",
            ctx.Status == WorkflowStatus.Completed ? Severity.Info : Severity.Error);

        return ctx;
    }

    private async Task ExecuteBatchesAsync(WorkflowContext ctx, DependencyGraph graph, CancellationToken ct)
    {
        var completed = new HashSet<StageName>();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var ready = graph.GetReadyStages(completed)
                             .Where(s => ctx.StageResults.GetValueOrDefault(s.Name)?.Status != StageStatus.Completed)
                             .ToList();

            if (ready.Count == 0) break;

            // Initialize stage results for this batch
            foreach (var def in ready)
            {
                if (!ctx.StageResults.ContainsKey(def.Name))
                    ctx.StageResults[def.Name] = new StageResult { Stage = def.Name };
            }

            // Run parallel-eligible stages concurrently; serial ones sequentially
            var parallelBatch = ready.Where(s => s.CanRunInParallel).ToList();
            var serialBatch   = ready.Where(s => !s.CanRunInParallel).ToList();

            if (parallelBatch.Count > 0)
            {
                var tasks = parallelBatch.Select(def => ExecuteStageAsync(ctx, def, ct));
                await Task.WhenAll(tasks);
            }

            foreach (var def in serialBatch)
                await ExecuteStageAsync(ctx, def, ct);

            // Re-plan: add any stage whose new status changed to Completed
            var justCompleted = ready
                .Where(s => ctx.StageResults.GetValueOrDefault(s.Name)?.Status == StageStatus.Completed)
                .Select(s => s.Name);
            foreach (var name in justCompleted) completed.Add(name);

            // If no progress was made, break to avoid infinite loop
            if (!completed.Overlaps(ready.Select(r => r.Name))) break;
        }
    }

    private async Task ExecuteStageAsync(WorkflowContext ctx, StageDefinition def, CancellationToken ct)
    {
        var result = ctx.StageResults[def.Name];

        // ── Entry gate ────────────────────────────────────────────────────────
        var (entryPassed, entryReason) = await def.EntryGate(ctx);
        if (!entryPassed)
        {
            result.Status = StageStatus.Skipped;
            auditLogger.Log(ctx, def.Name.ToString(), "EntryGateFailed", "gate",
                $"Entry gate rejected: {entryReason}", Severity.Warning);
            return;
        }

        // ── Policy guardrails ─────────────────────────────────────────────────
        var violations = guardrails.Evaluate(ctx, def.Name);
        foreach (var v in violations)
        {
            auditLogger.Log(ctx, def.Name.ToString(), "PolicyViolation", "guardrail",
                $"[{v.PolicyName}] {v.Description}", v.Severity);
            if (v.Severity == Severity.Critical)
            {
                result.Status = StageStatus.Failed;
                result.FailureReason = $"Policy violation: {v.PolicyName} — {v.Description}";
                return;
            }
        }

        // ── Execute with retry ────────────────────────────────────────────────
        result.Status = StageStatus.Running;
        metrics.RecordStageStart(ctx, def.Name);
        auditLogger.Log(ctx, def.Name.ToString(), "StageStarted", "orchestrator",
            $"Stage {def.Name} started.", Severity.Info);

        var succeeded = false;
        Exception? lastEx = null;

        for (var attempt = 1; attempt <= def.RetryPolicy.MaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (!_stages.TryGetValue(def.Name, out var stage))
                    throw new InvalidOperationException($"No implementation found for stage {def.Name}.");

                await stage.ExecuteAsync(ctx, result, ct);
                succeeded = true;
                break;
            }
            catch (Exception ex) when (def.RetryPolicy.ShouldRetry(ex, attempt))
            {
                lastEx = ex;
                metrics.RecordRetry(ctx, def.Name);
                var delay = def.RetryPolicy.GetDelay(attempt);
                logger.LogWarning(ex,
                    "Stage {Stage} attempt {Attempt}/{Max} failed. Retrying in {Delay}.",
                    def.Name, attempt, def.RetryPolicy.MaxAttempts, delay);
                auditLogger.Log(ctx, def.Name.ToString(), "RetryScheduled", "orchestrator",
                    $"Attempt {attempt} failed: {ex.Message}. Retry in {delay.TotalSeconds:F1}s.", Severity.Warning);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                break;
            }
        }

        if (!succeeded)
        {
            result.Status = StageStatus.Failed;
            result.FailureReason = lastEx?.Message ?? "Unknown failure";
            metrics.RecordStageEnd(ctx, def.Name, wasRecovered: false);
            auditLogger.Log(ctx, def.Name.ToString(), "StageFailed", "orchestrator",
                $"Stage failed after {result.RetryCount} retries: {result.FailureReason}", Severity.Error);

            if (def.SupportsRollback)
                await rollback.RollbackFromAsync(ctx, def.Name);

            if (ctx.Status == WorkflowStatus.Running)
                ctx.Status = WorkflowStatus.Failed;
            return;
        }

        // ── Exit gate ─────────────────────────────────────────────────────────
        var (exitPassed, exitMessages) = await def.ExitGate(ctx, result);
        result.ValidationMessages.AddRange(exitMessages);
        if (!exitPassed)
        {
            result.Status = StageStatus.Failed;
            result.FailureReason = $"Exit gate failed: {string.Join("; ", exitMessages)}";
            metrics.RecordStageEnd(ctx, def.Name, wasRecovered: false);
            auditLogger.Log(ctx, def.Name.ToString(), "ExitGateFailed", "gate",
                result.FailureReason, Severity.Error);
            return;
        }

        // ── Human approval ────────────────────────────────────────────────────
        if (def.RequiresHumanApproval)
        {
            result.Status = StageStatus.AwaitingApproval;
            ctx.Status = WorkflowStatus.AwaitingApproval;
            auditLogger.Log(ctx, def.Name.ToString(), "ApprovalRequested", "orchestrator",
                $"Stage {def.Name} awaiting human approval.", Severity.Info);

            var decision = await approvalService.RequestApprovalAsync(
                ctx, def.Name,
                $"Approve stage {def.Name} outputs?",
                result.Outputs.ToDictionary(k => k.Key, v => v.Value),
                ct);

            ctx.Status = WorkflowStatus.Running;

            if (decision != ApprovalDecision.Approved)
            {
                result.Status = StageStatus.Failed;
                result.FailureReason = $"Human approval {decision} for stage {def.Name}.";
                auditLogger.Log(ctx, def.Name.ToString(), "ApprovalRejected", "human",
                    $"Stage {def.Name} was {decision}.", Severity.Warning);
                return;
            }

            auditLogger.Log(ctx, def.Name.ToString(), "ApprovalGranted", "human",
                $"Stage {def.Name} approved by human reviewer.", Severity.Info);
        }

        // ── Success ───────────────────────────────────────────────────────────
        result.Status = StageStatus.Completed;
        metrics.RecordStageEnd(ctx, def.Name, wasRecovered: result.RetryCount > 0);
        auditLogger.Log(ctx, def.Name.ToString(), "StageCompleted", "orchestrator",
            $"Stage {def.Name} completed. Artifacts: [{string.Join(", ", result.Artifacts)}]",
            Severity.Info);
    }

    /// <summary>
    /// Builds the dependency graph for the given scenario.
    /// Testing and Documentation are parallel (no dependency between them).
    /// ReleaseReadiness depends on both completing.
    /// </summary>
    private DependencyGraph BuildGraph(ScenarioType scenario)
    {
        var graph = new DependencyGraph();

        graph.Add(new StageDefinition
        {
            Name = StageName.Requirements,
            DependsOn = [],
            RequiresHumanApproval = false,
            RetryPolicy = RetryPolicy.Default,
            EntryGate = EntryGate.AlwaysPass,
            ExitGate = ExitGate.RequireOutputs("normalized_spec")
        });

        graph.Add(new StageDefinition
        {
            Name = StageName.Architecture,
            DependsOn = [StageName.Requirements],
            RequiresHumanApproval = true, // Architecture changes are high-impact
            RetryPolicy = RetryPolicy.Default,
            EntryGate = EntryGate.RequireStagesCompleted(StageName.Requirements),
            ExitGate = ExitGate.RequireOutputs("adrs", "components", "api_schema"),
            SupportsRollback = false
        });

        graph.Add(new StageDefinition
        {
            Name = StageName.Implementation,
            DependsOn = [StageName.Architecture],
            RequiresHumanApproval = false,
            RetryPolicy = RetryPolicy.Default,
            EntryGate = EntryGate.RequireStagesCompleted(StageName.Architecture),
            ExitGate = ExitGate.RequireOutputs("code_artifacts"),
            SupportsRollback = true
        });

        // Testing and Documentation run in parallel after Implementation
        graph.Add(new StageDefinition
        {
            Name = StageName.Testing,
            DependsOn = [StageName.Implementation],
            CanRunInParallel = true,
            RetryPolicy = RetryPolicy.Default,
            EntryGate = EntryGate.RequireStagesCompleted(StageName.Implementation),
            ExitGate = ExitGate.RequireOutputs("test_plan", "all_tests_pass")
        });

        graph.Add(new StageDefinition
        {
            Name = StageName.Documentation,
            DependsOn = [StageName.Architecture],
            CanRunInParallel = true,
            RetryPolicy = RetryPolicy.Default,
            EntryGate = EntryGate.RequireStagesCompleted(StageName.Architecture),
            ExitGate = ExitGate.RequireArtifacts(1)
        });

        graph.Add(new StageDefinition
        {
            Name = StageName.ReleaseReadiness,
            DependsOn = [StageName.Testing, StageName.Documentation],
            RequiresHumanApproval = true, // No release without human sign-off
            RetryPolicy = RetryPolicy.NoRetry,
            EntryGate = EntryGate.RequireStagesCompleted(StageName.Testing, StageName.Documentation),
            ExitGate = ExitGate.RequireOutputs("release_ready"),
            SupportsRollback = true
        });

        return graph;
    }

    public Task SafeStopAsync(string workflowId, CancellationToken ct = default)
    {
        if (_stopTokens.TryGetValue(workflowId, out var cts))
            cts.Cancel();
        return Task.CompletedTask;
    }

    public WorkflowContext? GetStatus(string workflowId)
        => _workflows.GetValueOrDefault(workflowId);

    public IReadOnlyList<WorkflowContext> GetAllWorkflows()
        => [.. _workflows.Values];
}
