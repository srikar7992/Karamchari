namespace Karamchari.Payroll.Services;

using Karamchari.Payroll.Domain.SalaryStructures;

/// <summary>
/// A granular step in the compiled salary execution plan.
/// </summary>
public record EvaluationStep(
    Guid ComponentId,
    ComponentType Type,
    ComponentCalculationType CalcType,
    decimal? Value,
    List<Guid> Dependencies,
    DependencyAggregationType Aggregation,
    RoundingStrategy Rounding
);

/// <summary>
/// A perfectly ordered execution plan that avoids DAG evaluation during hot-path payroll runs.
/// </summary>
public record CompiledExecutionPlan(
    Guid TemplateId,
    List<EvaluationStep> OrderedSteps,
    EvaluationStep? RemainderStep
);

/// <summary>
/// Compiles a SalaryTemplate into an ordered execution plan using Kahn's Algorithm.
/// </summary>
public sealed class CTCTemplateCompiler
{
    /// <summary>
    /// Compiles the template and validates for circular dependencies.
    /// </summary>
    public static CompiledExecutionPlan Compile(SalaryTemplate template, List<SalaryComponent> masterComponents)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(masterComponents);

        // 1. Validation: Ensure exactly 0 or 1 Remainder
        var remainderComponents = template.Components.Where(c => c.CalculationType == ComponentCalculationType.Remainder).ToList();
        if (remainderComponents.Count > 1)
        {
            throw new InvalidOperationException("Only one remainder component is allowed per template.");
        }
        
        // 2. Kahn's Algorithm for Topological Sort & Cycle Detection
        var inDegree = new Dictionary<Guid, int>();
        var adjacencyList = new Dictionary<Guid, List<Guid>>();
        var componentMap = template.Components.ToDictionary(c => c.ComponentId);

        // Initialize graph
        foreach (var comp in template.Components)
        {
            if (!inDegree.ContainsKey(comp.ComponentId)) inDegree[comp.ComponentId] = 0;
            if (!adjacencyList.ContainsKey(comp.ComponentId)) adjacencyList[comp.ComponentId] = new List<Guid>();

            foreach (var depId in comp.DependsOnComponentIds)
            {
                if (!adjacencyList.ContainsKey(depId)) adjacencyList[depId] = new List<Guid>();
                adjacencyList[depId].Add(comp.ComponentId);
                
                if (!inDegree.ContainsKey(comp.ComponentId)) inDegree[comp.ComponentId] = 0;
                inDegree[comp.ComponentId]++;
            }
        }

        var queue = new Queue<Guid>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var sortedIds = new List<Guid>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sortedIds.Add(current);

            if (adjacencyList.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0) queue.Enqueue(neighbor);
                }
            }
        }

        // 3. Cycle Check
        if (sortedIds.Count != template.Components.Count)
        {
            throw new InvalidOperationException("Circular dependency detected in salary template graph.");
        }

        // 4. Stable Sort by Priority (Preserving topological order where dependencies aren't strict)
        // Note: ExecutionPriority allows users to force order among independent components
        var orderedSteps = sortedIds
            .Select(id => componentMap[id])
            .Where(c => c.CalculationType != ComponentCalculationType.Remainder) 
            .OrderBy(c => c.ExecutionPriority)
            .Select(c => MapToStep(c, masterComponents))
            .ToList();

        var remainderStep = remainderComponents.Count == 1 
            ? MapToStep(remainderComponents.First(), masterComponents) 
            : null;

        return new CompiledExecutionPlan(template.Id, orderedSteps, remainderStep);
    }

    private static EvaluationStep MapToStep(SalaryTemplateComponent tc, List<SalaryComponent> masters)
    {
        var master = masters.First(m => m.Id == tc.ComponentId);
        return new EvaluationStep(
            tc.ComponentId, 
            master.Type, 
            tc.CalculationType, 
            tc.Value, 
            tc.DependsOnComponentIds, 
            tc.AggregationType, 
            master.Rounding);
    }
}
