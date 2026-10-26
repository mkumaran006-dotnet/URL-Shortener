namespace UrlShortener.Orchestration.Models;

public class StageResult
{
    public StageName Stage { get; init; }
    public StageStatus Status { get; set; } = StageStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object?> Outputs { get; set; } = [];
    public List<string> Artifacts { get; set; } = [];
    public List<string> ValidationMessages { get; set; } = [];
}
