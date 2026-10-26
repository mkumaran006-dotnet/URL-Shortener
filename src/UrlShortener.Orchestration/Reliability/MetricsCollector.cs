using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Reliability;

public class MetricsCollector
{
    public void RecordStageStart(WorkflowContext ctx, StageName stage)
    {
        if (ctx.StageResults.TryGetValue(stage, out var result))
            result.StartedAt = DateTime.UtcNow;
    }

    public void RecordStageEnd(WorkflowContext ctx, StageName stage, bool wasRecovered = false)
    {
        if (!ctx.StageResults.TryGetValue(stage, out var result)) return;

        result.CompletedAt = DateTime.UtcNow;
        var duration = result.CompletedAt.Value - (result.StartedAt ?? result.CompletedAt.Value);

        if (result.Status == StageStatus.Completed)
            ctx.Metrics.CompletedStages++;
        else if (result.Status is StageStatus.Failed or StageStatus.RolledBack)
            ctx.Metrics.FailedStages++;

        var recoverySeconds = wasRecovered ? duration.TotalSeconds : 0;
        ctx.Metrics.StageDurations.Add(new StageDuration(
            stage.ToString(),
            result.StartedAt ?? result.CompletedAt.Value,
            result.CompletedAt.Value,
            wasRecovered,
            recoverySeconds));
    }

    public void RecordRetry(WorkflowContext ctx, StageName stage)
    {
        ctx.Metrics.TotalRetries++;
        if (ctx.StageResults.TryGetValue(stage, out var result))
            result.RetryCount++;
    }

    public void FinalizeWorkflow(WorkflowContext ctx)
        => ctx.Metrics.CompletedAt = DateTime.UtcNow;
}
