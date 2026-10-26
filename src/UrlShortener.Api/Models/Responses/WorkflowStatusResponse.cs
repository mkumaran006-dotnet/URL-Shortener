namespace UrlShortener.Api.Models.Responses;

public record WorkflowStatusResponse(
    string WorkflowId,
    string ScenarioType,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    IReadOnlyList<StageStatusResponse> Stages,
    WorkflowMetricsResponse Metrics,
    IReadOnlyList<AuditEntryResponse> AuditLog);

public record StageStatusResponse(
    string StageName,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    int RetryCount,
    IReadOnlyList<string> Artifacts);

public record WorkflowMetricsResponse(
    double SuccessRate,
    int TotalRetries,
    int TotalRollbacks,
    TimeSpan TotalDuration,
    double MttrSeconds);

public record AuditEntryResponse(
    DateTime Timestamp,
    string Stage,
    string Action,
    string Actor,
    string Details,
    string Severity);
