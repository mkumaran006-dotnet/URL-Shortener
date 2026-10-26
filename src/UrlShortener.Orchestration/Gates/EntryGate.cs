using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Gates;

/// <summary>
/// Validates pre-conditions before a stage may begin.
/// Returns (passed, reason). Reason is null when passed.
/// </summary>
public delegate Task<(bool Passed, string? Reason)> EntryGateDelegate(WorkflowContext ctx);

public static class EntryGate
{
    public static readonly EntryGateDelegate AlwaysPass =
        _ => Task.FromResult<(bool, string?)>((true, null));

    /// <summary>
    /// Gate that requires all listed upstream stages to be in Completed status.
    /// </summary>
    public static EntryGateDelegate RequireStagesCompleted(params StageName[] required) =>
        ctx =>
        {
            foreach (var stage in required)
            {
                if (!ctx.StageResults.TryGetValue(stage, out var r) || r.Status != StageStatus.Completed)
                    return Task.FromResult<(bool, string?)>(
                        (false, $"Stage {stage} must be completed before this stage can begin."));
            }
            return Task.FromResult<(bool, string?)>((true, null));
        };

    /// <summary>
    /// Gate that requires a specific output key to exist from an upstream stage.
    /// </summary>
    public static EntryGateDelegate RequireOutput(StageName stage, string outputKey) =>
        ctx =>
        {
            var value = ctx.GetOutput<object>(stage, outputKey);
            return Task.FromResult<(bool, string?)>(
                value is not null
                    ? (true, null)
                    : (false, $"Required output '{outputKey}' from stage {stage} is missing."));
        };

    /// <summary>
    /// Composes multiple gates — all must pass.
    /// </summary>
    public static EntryGateDelegate All(params EntryGateDelegate[] gates) =>
        async ctx =>
        {
            foreach (var gate in gates)
            {
                var (passed, reason) = await gate(ctx);
                if (!passed) return (false, reason);
            }
            return (true, null);
        };
}
