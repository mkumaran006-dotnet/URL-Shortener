using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// A discrete unit of work in the SDLC workflow.
/// Each stage reads from WorkflowContext (upstream outputs) and writes its own outputs back.
/// </summary>
public interface IStage
{
    StageName Name { get; }

    Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default);

    Task RollbackAsync(WorkflowContext ctx, CancellationToken ct = default)
        => Task.CompletedTask;
}
