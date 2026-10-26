using UrlShortener.Orchestration.Models;

namespace UrlShortener.Orchestration.Engine;

/// <summary>
/// Directed Acyclic Graph of SDLC stage definitions.
/// Supports topological sort and parallel-group extraction.
/// </summary>
public class DependencyGraph
{
    private readonly Dictionary<StageName, StageDefinition> _nodes = new();
    private readonly Dictionary<StageName, HashSet<StageName>> _edges = new();

    public DependencyGraph Add(StageDefinition definition)
    {
        _nodes[definition.Name] = definition;
        _edges.TryAdd(definition.Name, []);
        foreach (var dep in definition.DependsOn)
        {
            _edges.TryAdd(dep, []);
            _edges[dep].Add(definition.Name);
        }
        return this;
    }

    public IReadOnlyList<StageDefinition> GetReadyStages(ISet<StageName> completedStages)
        => _nodes.Values
                 .Where(n => !completedStages.Contains(n.Name) &&
                             n.DependsOn.All(completedStages.Contains))
                 .ToList();

    public IReadOnlyList<IReadOnlyList<StageDefinition>> GetParallelBatches()
    {
        var sorted = TopologicalSort();
        var batches = new List<IReadOnlyList<StageDefinition>>();
        var scheduled = new HashSet<StageName>();

        while (scheduled.Count < sorted.Count)
        {
            var batch = sorted
                .Where(s => !scheduled.Contains(s.Name) &&
                            s.DependsOn.All(scheduled.Contains))
                .ToList();

            if (batch.Count == 0) break;
            batches.Add(batch);
            foreach (var s in batch) scheduled.Add(s.Name);
        }

        return batches;
    }

    public IReadOnlyList<StageDefinition> TopologicalSort()
    {
        var visited = new HashSet<StageName>();
        var stack = new Stack<StageDefinition>();

        foreach (var node in _nodes.Values)
            if (!visited.Contains(node.Name))
                Visit(node.Name, visited, stack);

        // Stack enumerates top→bottom; DependsOn-based DFS produces correct order bottom→top, so reverse.
        return [.. stack.Reverse()];
    }

    private void Visit(StageName name, HashSet<StageName> visited, Stack<StageDefinition> stack)
    {
        visited.Add(name);
        foreach (var dep in _nodes[name].DependsOn)
            if (!visited.Contains(dep))
                Visit(dep, visited, stack);
        stack.Push(_nodes[name]);
    }

    public bool Validate(out IReadOnlyList<string> errors)
    {
        var errs = new List<string>();

        // Check for missing dependency declarations
        foreach (var node in _nodes.Values)
            foreach (var dep in node.DependsOn)
                if (!_nodes.ContainsKey(dep))
                    errs.Add($"Stage {node.Name} depends on unknown stage {dep}.");

        // Check for cycles using DFS
        var color = _nodes.Keys.ToDictionary(k => k, _ => 0); // 0=white, 1=gray, 2=black
        foreach (var node in _nodes.Keys)
            if (color[node] == 0 && HasCycle(node, color))
                errs.Add($"Cycle detected involving stage {node}.");

        errors = errs;
        return errs.Count == 0;
    }

    private bool HasCycle(StageName node, Dictionary<StageName, int> color)
    {
        color[node] = 1;
        foreach (var neighbor in _nodes[node].DependsOn)
        {
            if (!color.ContainsKey(neighbor)) continue;
            if (color[neighbor] == 1) return true;
            if (color[neighbor] == 0 && HasCycle(neighbor, color)) return true;
        }
        color[node] = 2;
        return false;
    }

    public StageDefinition? GetDefinition(StageName name)
        => _nodes.GetValueOrDefault(name);

    public IReadOnlyList<StageDefinition> GetAllDefinitions() => [.. _nodes.Values];
}
