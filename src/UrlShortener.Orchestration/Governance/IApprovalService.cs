using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Governance;

public interface IApprovalService
{
    Task<ApprovalDecision> RequestApprovalAsync(
        WorkflowContext ctx,
        StageName stage,
        string description,
        IReadOnlyDictionary<string, object?> artifacts,
        CancellationToken ct = default);
}
