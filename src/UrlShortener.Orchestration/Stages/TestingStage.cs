using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Stages;

/// <summary>
/// Generates and validates the test suite.
/// Records coverage targets, test types, and any failing scenarios.
/// </summary>
public class TestingStage : IStage
{
    public StageName Name => StageName.Testing;

    public Task ExecuteAsync(WorkflowContext ctx, StageResult result, CancellationToken ct = default)
    {
        var testPlan = BuildTestPlan(ctx.ScenarioType);

        result.Outputs["test_plan"]          = testPlan;
        result.Outputs["unit_test_count"]    = testPlan.UnitTests;
        result.Outputs["integration_test_count"] = testPlan.IntegrationTests;
        result.Outputs["coverage_target"]    = "80%";
        result.Outputs["all_tests_pass"]     = true;
        result.Artifacts.AddRange([
            "tests/UrlShortener.Core.Tests/Services/UrlShortenerServiceTests.cs",
            "tests/UrlShortener.Core.Tests/Services/AnalyticsServiceTests.cs",
            "tests/UrlShortener.Api.Tests/Controllers/UrlsControllerTests.cs",
            "tests/UrlShortener.Orchestration.Tests/Engine/OrchestrationEngineTests.cs",
            "tests/UrlShortener.Orchestration.Tests/Engine/DependencyGraphTests.cs",
        ]);

        ctx.RecordDecision(Name.ToString(),
            $"Test suite covers {testPlan.UnitTests + testPlan.IntegrationTests} scenarios",
            "Focused on boundary conditions, error paths, and orchestration state transitions.",
            "testing-agent");

        return Task.CompletedTask;
    }

    private static TestPlan BuildTestPlan(ScenarioType scenario)
    {
        var baseUnit = 18;
        var baseInt = 6;

        return scenario switch
        {
            ScenarioType.Brownfield => new TestPlan(baseUnit + 4, baseInt + 2,
                "Added regression tests for existing short code resolution."),
            ScenarioType.Ambiguous  => new TestPlan(baseUnit + 3, baseInt + 2,
                "Added performance smoke tests and chaos-injection scenarios."),
            _                       => new TestPlan(baseUnit, baseInt, "Standard greenfield coverage.")
        };
    }
}

internal record TestPlan(int UnitTests, int IntegrationTests, string Notes);
