namespace UrlShortener.Orchestration.Models;

public record AuditEntry(
    DateTime Timestamp,
    string WorkflowId,
    string Stage,
    string Action,
    string Actor,
    string Details,
    Severity Severity);
