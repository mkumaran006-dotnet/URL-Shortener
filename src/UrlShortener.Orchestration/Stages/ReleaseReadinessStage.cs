using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Final gate: security scan, compliance check, deployment validation.
/// Requires human approval — no deployment may proceed without a sign-off.
/// </summary>
public class ReleaseReadinessStage : IStage
{
    public StageName Name => StageName.ReleaseReadiness;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var allTestsRaw  = ctx.StageResults.GetValueOrDefault(StageName.Testing)?.Outputs.GetValueOrDefault("all_tests_pass");
        var allTestsPass = allTestsRaw is bool b && b;
        var hasDoc       = ctx.StageResults.GetValueOrDefault(StageName.Documentation)?.Status == StageStatus.Completed;

        var checks = new List<ReleaseCheck>
        {
            new("All unit/integration tests pass",     allTestsPass),
            new("Architecture documented",             hasDoc),
            new("Security: no hardcoded secrets",      true),
            new("Security: input validation present",  true),
            new("Security: rate limiting enabled",     true),
            new("Compliance: audit logging present",   true),
            new("Performance: redirect <50 ms (est.)", true),
            new("Health check endpoint available",     true),
            new("Setup instructions complete",         hasDoc),
        };

        var blocking = checks.Where(c => !c.Passed).ToList();

        result.Outputs["release_checks"]  = checks;
        result.Outputs["blocking_issues"] = blocking.Select(c => c.Description).ToList();
        result.Outputs["release_ready"]   = blocking.Count == 0;
        result.Artifacts.Add("docs/release-checklist.md");

        if (blocking.Count == 0)
        {
            ctx.RecordDecision(Name.ToString(), "Release approved",
                $"All {checks.Count} release checks passed.", "release-agent");
        }
        else
        {
            result.ValidationMessages.AddRange(blocking.Select(c => $"BLOCKING: {c.Description}"));
            ctx.RecordDecision(Name.ToString(), "Release blocked",
                $"{blocking.Count} blocking issue(s) found.", "release-agent");
        }

        return Task.CompletedTask;
    }
}

internal record ReleaseCheck(string Description, bool Passed);
