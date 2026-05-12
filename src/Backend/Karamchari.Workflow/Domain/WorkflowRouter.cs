using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Simple routing request containing basic entity metadata for workflow selection.
/// </summary>
public sealed record WorkflowRoutingRequest(
    string EntityType,
    string TenantId,
    string? Category = null,
    decimal? Amount = null);

/// <summary>
/// Selects the correct <see cref="WorkflowDefinition"/> for an entity at submission time.
/// </summary>
public sealed class WorkflowRouter
{
    /// <summary>
    /// Returns the highest-priority active definition that matches the entity type.
    /// Tie-breaking is deterministic: higher Priority wins, then alphabetically by Name.
    /// </summary>
    public static WorkflowDefinition? Route(
        IEnumerable<WorkflowDefinition> candidates,
        WorkflowRoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(request);

        return candidates
            .Where(d => d.IsActive && d.EntityType == request.EntityType)
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
