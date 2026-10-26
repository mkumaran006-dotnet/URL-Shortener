using UrlShortener.Orchestration.Engine;
using UrlShortener.Orchestration.Gates;
using UrlShortener.Orchestration.Models;
using UrlShortener.Orchestration.Reliability;

namespace UrlShortener.Orchestration.Tests.Engine;

public class DependencyGraphTests
{
    private static DependencyGraph BuildStandardGraph()
    {
        var graph = new DependencyGraph();
        graph.Add(new StageDefinition { Name = StageName.Requirements, DependsOn = [] });
        graph.Add(new StageDefinition { Name = StageName.Architecture, DependsOn = [StageName.Requirements] });
        graph.Add(new StageDefinition { Name = StageName.Implementation, DependsOn = [StageName.Architecture] });
        graph.Add(new StageDefinition { Name = StageName.Testing, DependsOn = [StageName.Implementation], CanRunInParallel = true });
        graph.Add(new StageDefinition { Name = StageName.Documentation, DependsOn = [StageName.Architecture], CanRunInParallel = true });
        graph.Add(new StageDefinition { Name = StageName.ReleaseReadiness, DependsOn = [StageName.Testing, StageName.Documentation] });
        return graph;
    }

    [Fact]
    public void TopologicalSort_ProducesValidOrder()
    {
        var graph = BuildStandardGraph();
        var sorted = graph.TopologicalSort();

        Assert.Equal(6, sorted.Count);

        int IndexOf(StageName n) => sorted.ToList().FindIndex(s => s.Name == n);

        Assert.True(IndexOf(StageName.Requirements) < IndexOf(StageName.Architecture));
        Assert.True(IndexOf(StageName.Architecture) < IndexOf(StageName.Implementation));
        Assert.True(IndexOf(StageName.Implementation) < IndexOf(StageName.Testing));
        Assert.True(IndexOf(StageName.Testing) < IndexOf(StageName.ReleaseReadiness));
        Assert.True(IndexOf(StageName.Documentation) < IndexOf(StageName.ReleaseReadiness));
    }

    [Fact]
    public void GetReadyStages_ReturnsOnlyRequirements_WhenNothingCompleted()
    {
        var graph = BuildStandardGraph();
        var ready = graph.GetReadyStages(new HashSet<StageName>());

        Assert.Single(ready);
        Assert.Equal(StageName.Requirements, ready[0].Name);
    }

    [Fact]
    public void GetReadyStages_ReturnsTwoParallelStages_AfterImplementation()
    {
        var graph = BuildStandardGraph();
        var completed = new HashSet<StageName>
        {
            StageName.Requirements,
            StageName.Architecture,
            StageName.Implementation
        };

        var ready = graph.GetReadyStages(completed);

        Assert.Equal(2, ready.Count);
        Assert.Contains(ready, s => s.Name == StageName.Testing);
        Assert.Contains(ready, s => s.Name == StageName.Documentation);
    }

    [Fact]
    public void Validate_Passes_ForValidGraph()
    {
        var graph = BuildStandardGraph();
        Assert.True(graph.Validate(out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void GetParallelBatches_ReturnsImplementationAndDocInSameBatch()
    {
        var graph = BuildStandardGraph();
        var batches = graph.GetParallelBatches();

        // Documentation depends only on Architecture; Implementation also depends only on Architecture.
        // Both become ready at the same time, so they share a batch.
        var implBatch = batches.First(b => b.Any(s => s.Name == StageName.Implementation));
        Assert.Contains(implBatch, s => s.Name == StageName.Documentation);
    }
}
