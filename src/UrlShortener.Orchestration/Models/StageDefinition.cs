using UrlShortener.Orchestration.Gates;
using UrlShortener.Orchestration.Reliability;

namespace UrlShortener.Orchestration.Models;

public class StageDefinition
{
    public StageName Name { get; init; }
    public IReadOnlyList<StageName> DependsOn { get; init; } = [];
    public bool RequiresHumanApproval { get; init; }
    public bool CanRunInParallel { get; init; }
    public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Default;
    public EntryGateDelegate EntryGate { get; init; } = Gates.EntryGate.AlwaysPass;
    public ExitGateDelegate ExitGate { get; init; } = Gates.ExitGate.AlwaysPass;
    public bool SupportsRollback { get; init; }
}
