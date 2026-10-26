using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Governance;

public interface IAuditLogger
{
    void Log(WorkflowContext ctx, string stage, string action, string actor, string details, Severity severity = Severity.Info);
    IReadOnlyList<AuditEntry> GetLog(string workflowId);
}
