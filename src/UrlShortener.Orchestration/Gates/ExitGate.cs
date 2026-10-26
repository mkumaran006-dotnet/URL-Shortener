using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Gates;

/// <summary>
/// Validates post-conditions after a stage completes.
/// Returns (passed, validationMessages).
/// </summary>
public delegate Task<(bool Passed, IReadOnlyList<string> Messages)> ExitGateDelegate(WorkflowContext ctx, StageResult result);

public static class ExitGate
{
    public static readonly ExitGateDelegate AlwaysPass =
        (_, _) => Task.FromResult<(bool, IReadOnlyList<string>)>((true, Array.Empty<string>()));

    /// <summary>
    /// Requires that a set of output keys are present in the stage result.
    /// </summary>
    public static ExitGateDelegate RequireOutputs(params string[] keys) =>
        (_, result) =>
        {
            var missing = keys.Where(k => !result.Outputs.ContainsKey(k) || result.Outputs[k] is null).ToList();
            return Task.FromResult<(bool, IReadOnlyList<string>)>(
                missing.Count == 0
                    ? (true, Array.Empty<string>())
                    : (false, missing.Select(k => $"Required output '{k}' was not produced.").ToList()));
        };

    /// <summary>
    /// Validates that produced artifacts are not empty strings.
    /// </summary>
    public static ExitGateDelegate RequireArtifacts(int minimumCount = 1) =>
        (_, result) =>
        {
            var valid = result.Artifacts.Count >= minimumCount && result.Artifacts.All(a => !string.IsNullOrWhiteSpace(a));
            return Task.FromResult<(bool, IReadOnlyList<string>)>(
                valid
                    ? (true, Array.Empty<string>())
                    : (false, new[] { $"Stage must produce at least {minimumCount} artifact(s)." }));
        };

    /// <summary>
    /// Composes multiple exit gates — all must pass.
    /// </summary>
    public static ExitGateDelegate All(params ExitGateDelegate[] gates) =>
        async (ctx, result) =>
        {
            var all = new List<string>();
            foreach (var gate in gates)
            {
                var (passed, msgs) = await gate(ctx, result);
                if (!passed) all.AddRange(msgs);
            }
            return (all.Count == 0, all);
        };
}
