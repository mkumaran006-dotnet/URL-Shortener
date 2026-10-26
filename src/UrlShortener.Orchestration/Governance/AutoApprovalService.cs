using Microsoft.Extensions.Logging;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Governance;

/// <summary>
/// Auto-approves all requests after a configurable simulated review delay.
/// Used in demo/test mode; swap for a real approval workflow in production.
/// </summary>
public class AutoApprovalService(ILogger<AutoApprovalService> logger, TimeSpan? reviewDelay = null) : IApprovalService
{
    private readonly TimeSpan _delay = reviewDelay ?? TimeSpan.FromMilliseconds(100);

    public async Task<ApprovalDecision> RequestApprovalAsync(
        WorkflowContext ctx,
        StageName stage,
        string description,
        IReadOnlyDictionary<string, object?> artifacts,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[APPROVAL REQUEST] Workflow={WorkflowId} Stage={Stage} — {Description}. Artifacts: {Count}",
            ctx.WorkflowId, stage, description, artifacts.Count);

        await Task.Delay(_delay, ct);

        logger.LogInformation("[APPROVAL GRANTED] Workflow={WorkflowId} Stage={Stage}", ctx.WorkflowId, stage);
        return ApprovalDecision.Approved;
    }
}
