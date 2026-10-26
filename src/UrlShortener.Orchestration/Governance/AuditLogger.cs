using Microsoft.Extensions.Logging;
using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Governance;

public class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    private readonly List<AuditEntry> _entries = [];
    private readonly object _lock = new();

    public void Log(WorkflowContext ctx, string stage, string action, string actor,
        string details, Severity severity = Severity.Info)
    {
        var entry = new AuditEntry(DateTime.UtcNow, ctx.WorkflowId, stage, action, actor, details, severity);

        lock (_lock) _entries.Add(entry);

        var logLevel = severity switch
        {
            Severity.Critical => LogLevel.Critical,
            Severity.Error    => LogLevel.Error,
            Severity.Warning  => LogLevel.Warning,
            _                 => LogLevel.Information
        };

        logger.Log(logLevel, "[AUDIT] [{WorkflowId}] [{Stage}] {Action} by {Actor}: {Details}",
            ctx.WorkflowId, stage, action, actor, details);

        // Mirror into the context so the full audit trail travels with the workflow
        ctx.AuditLog.Add(entry);
    }

    public IReadOnlyList<AuditEntry> GetLog(string workflowId)
    {
        lock (_lock) return _entries.Where(e => e.WorkflowId == workflowId).ToList();
    }
}
