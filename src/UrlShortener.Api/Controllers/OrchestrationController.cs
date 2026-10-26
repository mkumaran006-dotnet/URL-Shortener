using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models.Responses;
using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Models;
using UrlShortener.Orchestration.Scenarios;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/orchestration")]
[Produces("application/json")]
public class OrchestrationController(
    IOrchestrationEngine engine,
    GreenfieldScenario greenfield,
    BrownfieldScenario brownfield,
    AmbiguousScenario ambiguous) : ControllerBase
{
    [HttpPost("run/{scenarioType}")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), 200)]
    public async Task<IActionResult> Run(string scenarioType, CancellationToken ct)
    {
        WorkflowContext ctx = scenarioType.ToLowerInvariant() switch
        {
            "greenfield" => await greenfield.RunAsync(ct),
            "brownfield" => await brownfield.RunAsync(ct),
            "ambiguous"  => await ambiguous.RunAsync(ct),
            _ => throw new ArgumentException($"Unknown scenario type '{scenarioType}'. Use: greenfield, brownfield, ambiguous.")
        };

        return Ok(ToResponse(ctx));
    }

    [HttpGet("{workflowId}")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), 200)]
    [ProducesResponseType(404)]
    public IActionResult GetStatus(string workflowId)
    {
        var ctx = engine.GetStatus(workflowId);
        return ctx is null ? NotFound() : Ok(ToResponse(ctx));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowStatusResponse>), 200)]
    public IActionResult GetAll()
        => Ok(engine.GetAllWorkflows().Select(ToResponse));

    [HttpDelete("{workflowId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SafeStop(string workflowId, CancellationToken ct)
    {
        await engine.SafeStopAsync(workflowId, ct);
        return NoContent();
    }

    private static WorkflowStatusResponse ToResponse(WorkflowContext ctx) =>
        new(
            ctx.WorkflowId,
            ctx.ScenarioType.ToString(),
            ctx.Status.ToString(),
            ctx.Metrics.StartedAt,
            ctx.Metrics.CompletedAt,
            ctx.StageResults.Values.Select(r => new StageStatusResponse(
                r.Stage.ToString(),
                r.Status.ToString(),
                r.StartedAt,
                r.CompletedAt,
                r.FailureReason,
                r.RetryCount,
                r.Artifacts)).ToList(),
            new WorkflowMetricsResponse(
                ctx.Metrics.SuccessRate,
                ctx.Metrics.TotalRetries,
                ctx.Metrics.TotalRollbacks,
                ctx.Metrics.TotalDuration,
                ctx.Metrics.MttrSeconds),
            ctx.AuditLog.Select(e => new AuditEntryResponse(
                e.Timestamp, e.Stage, e.Action, e.Actor, e.Details, e.Severity.ToString())).ToList());
}
