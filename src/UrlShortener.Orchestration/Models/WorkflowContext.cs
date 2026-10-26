namespace UrlShortener.Orchestration.Models;

public class WorkflowContext
{
    public string WorkflowId { get; init; } = Guid.NewGuid().ToString("N")[..12];
    public ScenarioType ScenarioType { get; init; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public Dictionary<string, object?> Input { get; init; } = [];
    public Dictionary<StageName, StageResult> StageResults { get; init; } = [];
    public List<DecisionRecord> DecisionLineage { get; init; } = [];
    public List<AuditEntry> AuditLog { get; init; } = [];
    public WorkflowMetrics Metrics { get; init; } = new();

    public T? GetOutput<T>(StageName stage, string key) where T : class
    {
        if (!StageResults.TryGetValue(stage, out var result)) return null;
        return result.Outputs.TryGetValue(key, out var val) ? val as T : null;
    }

    public void SetStageOutput(StageName stage, string key, object? value)
    {
        if (!StageResults.TryGetValue(stage, out var result)) return;
        result.Outputs[key] = value;
    }

    public void RecordDecision(string stage, string decision, string rationale,
        string madeBy = "agent", Dictionary<string, object?>? context = null)
    {
        DecisionLineage.Add(new DecisionRecord(
            DateTime.UtcNow, stage, decision, rationale, madeBy,
            context ?? new Dictionary<string, object?>()));
    }
}
