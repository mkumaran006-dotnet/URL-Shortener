using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Engine;

public interface IOrchestrationEngine
{
    Task<WorkflowContext> RunAsync(ScenarioType scenario, Dictionary<string, object?>? input = null, CancellationToken ct = default);

    Task SafeStopAsync(string workflowId, CancellationToken ct = default);

    WorkflowContext? GetStatus(string workflowId);

    IReadOnlyList<WorkflowContext> GetAllWorkflows();
}
