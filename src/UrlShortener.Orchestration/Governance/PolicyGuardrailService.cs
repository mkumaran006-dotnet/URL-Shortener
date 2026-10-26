using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Governance;

public record PolicyViolation(string PolicyName, string Description, Severity Severity);

public class PolicyGuardrailService
{
    private readonly List<Func<WorkflowContext, StageName, IReadOnlyList<PolicyViolation>>> _policies = [];

    public PolicyGuardrailService RegisterSecurityPolicy()
    {
        _policies.Add((ctx, stage) =>
        {
            var violations = new List<PolicyViolation>();

            if (stage == StageName.ReleaseReadiness)
            {
                var code = ctx.GetOutput<string>(StageName.Implementation, "code_artifacts");
                if (code?.Contains("password", StringComparison.OrdinalIgnoreCase) == true &&
                    !code.Contains("hash", StringComparison.OrdinalIgnoreCase))
                    violations.Add(new PolicyViolation("SEC-001", "Potential plaintext password detected in code.", Severity.Critical));
            }

            return violations;
        });
        return this;
    }

    public PolicyGuardrailService RegisterChangeControlPolicy()
    {
        _policies.Add((ctx, stage) =>
        {
            var violations = new List<PolicyViolation>();

            // Architecture decisions must be documented before Implementation
            if (stage == StageName.Implementation &&
                !ctx.StageResults.TryGetValue(StageName.Architecture, out var archResult) ||
                ctx.StageResults.GetValueOrDefault(StageName.Architecture)?.Status != StageStatus.Completed)
                violations.Add(new PolicyViolation("CC-001",
                    "Architecture stage must be approved before implementation begins.", Severity.Error));

            return violations;
        });
        return this;
    }

    public IReadOnlyList<PolicyViolation> Evaluate(WorkflowContext ctx, StageName stage)
        => _policies.SelectMany(p => p(ctx, stage)).ToList();

    public static PolicyGuardrailService CreateDefault()
        => new PolicyGuardrailService()
            .RegisterSecurityPolicy()
            .RegisterChangeControlPolicy();
}
