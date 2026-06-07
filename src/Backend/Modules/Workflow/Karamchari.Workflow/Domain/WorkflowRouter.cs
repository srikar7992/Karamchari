using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Routing request. <see cref="Context"/> is the attribute dictionary used to evaluate
/// <see cref="WorkflowCondition"/> expressions on each candidate definition.
/// Keys are case-sensitive strings matching the <c>Field</c> property of each condition.
/// Common keys: <c>Amount</c>, <c>EmployeeType</c>, <c>Location</c>, <c>Department</c>, <c>Tenure</c>.
/// </summary>
public sealed record WorkflowRoutingRequest(
    string EntityType,
    string TenantId,
    IReadOnlyDictionary<string, object>? Context = null);

/// <summary>
/// Selects the correct <see cref="WorkflowDefinition"/> for an entity at submission time.
/// Definitions are scored by Priority desc, then Name asc (deterministic tie-break).
/// Conditions within a definition use AND semantics; definitions with no conditions always match.
/// </summary>
public sealed class WorkflowRouter
{
    /// <summary>
    /// Returns the highest-priority active definition whose conditions all match the routing context.
    /// Returns <c>null</c> when no definition matches.
    /// </summary>
    public static WorkflowDefinition? Route(
        IEnumerable<WorkflowDefinition> candidates,
        WorkflowRoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(request);

        return candidates
            .Where(d => d.IsActive
                && d.EntityType == request.EntityType
                && d.Matches(request.Context))
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
