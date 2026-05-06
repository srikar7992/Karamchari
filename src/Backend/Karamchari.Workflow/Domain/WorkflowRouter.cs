using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Selects the correct <see cref="WorkflowDefinition"/> for an entity at submission time.
/// Evaluates optional condition expressions against the <see cref="Services.ApprovalContext"/>
/// so a single entity type can route differently based on amount, department, or billing flags.
/// </summary>
public sealed class WorkflowRouter
{
    /// <summary>
    /// Returns the highest-priority active definition that matches the context.
    /// Tie-breaking is deterministic: higher Priority wins, then alphabetically by Name.
    /// Returns null if no definition matches (caller decides: allow or block submission).
    /// </summary>
    public static WorkflowDefinition? Route(
        IEnumerable<WorkflowDefinition> candidates,
        Services.ApprovalContext context)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(context);

        return candidates
            .Where(d => d.IsActive && d.EntityType == context.EntityType)
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .FirstOrDefault(d => EvaluateCondition(d, context));
    }

    private static bool EvaluateCondition(WorkflowDefinition definition, Services.ApprovalContext context)
    {
        if (string.IsNullOrWhiteSpace(definition.ConditionJson))
            return true;

        try
        {
            var func = Services.WorkflowConditionCompiler.Compile(definition.Id, definition.ConditionJson);
            return func(context);
        }
        catch (Services.WorkflowConditionException)
        {
            return false;
        }
    }
}
