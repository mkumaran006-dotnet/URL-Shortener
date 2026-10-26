using Microsoft.Extensions.Logging;
using UrlShortener.Orchestration.Governance;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Reliability;

public class RollbackManager(IAuditLogger auditLogger, ILogger<RollbackManager> logger)
{
    private readonly Dictionary<StageName, Func<WorkflowContext, Task>> _rollbackHandlers = new();

    public void Register(StageName stage, Func<WorkflowContext, Task> handler)
        => _rollbackHandlers[stage] = handler;

    public async Task RollbackFromAsync(WorkflowContext ctx, StageName failedStage)
    {
        // Roll back in reverse topological order from the failed stage
        var toRollback = ctx.StageResults
            .Where(kvp => kvp.Value.Status == StageStatus.Completed && kvp.Key != failedStage)
            .OrderByDescending(kvp => kvp.Value.CompletedAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var stage in toRollback)
        {
            if (!_rollbackHandlers.TryGetValue(stage, out var handler))
            {
                logger.LogWarning("No rollback handler registered for stage {Stage}", stage);
                continue;
            }

            try
            {
                logger.LogInformation("Rolling back stage {Stage}", stage);
                await handler(ctx);
                ctx.StageResults[stage].Status = StageStatus.RolledBack;
                auditLogger.Log(ctx, stage.ToString(), "Rollback", "system",
                    $"Stage {stage} successfully rolled back.", Severity.Warning);
                ctx.Metrics.TotalRollbacks++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rollback failed for stage {Stage}", stage);
                auditLogger.Log(ctx, stage.ToString(), "RollbackFailed", "system",
                    $"Rollback for stage {stage} failed: {ex.Message}", Severity.Critical);
            }
        }
    }
}
