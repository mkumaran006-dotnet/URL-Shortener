using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Generates API docs, architecture overview, setup instructions, and decision summary.
/// Runs in parallel with Testing since it has no dependency on test results.
/// </summary>
public class DocumentationStage : IStage
{
    public StageName Name => StageName.Documentation;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var techStack = ctx.GetOutput<string>(StageName.Architecture, "tech_stack") ?? "N/A";
        var apiSchema = ctx.GetOutput<string>(StageName.Architecture, "api_schema") ?? "N/A";

        result.Outputs["readme_generated"]       = true;
        result.Outputs["openapi_generated"]      = true;
        result.Outputs["architecture_doc"]       = BuildArchSummary(techStack, ctx.ScenarioType);
        result.Outputs["decision_count"]         = ctx.DecisionLineage.Count;
        result.Artifacts.AddRange([
            "README.md",
            "docs/architecture.md",
            "docs/setup.md",
            $"docs/scenarios/{ctx.ScenarioType.ToString().ToLower()}.md",
        ]);

        return Task.CompletedTask;
    }

    private static string BuildArchSummary(string techStack, ScenarioType scenario) =>
        $"""
        ## Architecture Summary ({scenario})
        Stack: {techStack}
        Layers: Core (domain) → Infrastructure (data) → API (presentation)
        Orchestration: Dependency-graph driven with entry/exit gates, approval checkpoints, retry/rollback.
        """;
}
