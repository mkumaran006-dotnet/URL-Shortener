namespace UrlShortener.Orchestration.Models;

public class WorkflowMetrics
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalStages { get; set; }
    public int CompletedStages { get; set; }
    public int FailedStages { get; set; }
    public int TotalRetries { get; set; }
    public int TotalRollbacks { get; set; }
    public List<StageDuration> StageDurations { get; set; } = [];

    public double SuccessRate => TotalStages == 0 ? 0
        : (double)CompletedStages / TotalStages * 100;

    public TimeSpan TotalDuration => (CompletedAt ?? DateTime.UtcNow) - StartedAt;

    public double MttrSeconds => FailedStages == 0 ? 0
        : StageDurations.Where(d => d.WasRecovered).Average(d => d.RecoverySeconds);
}

public record StageDuration(
    string StageName,
    DateTime StartedAt,
    DateTime CompletedAt,
    bool WasRecovered,
    double RecoverySeconds);
