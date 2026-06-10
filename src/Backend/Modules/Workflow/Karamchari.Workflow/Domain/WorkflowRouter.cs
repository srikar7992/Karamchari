// -----------------------------------------------------------------------
// <copyright file="WorkflowRouter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Workflow.Domain;

/// <summary>
/// Routing request. <see cref="Context"/> is the attribute dictionary used to evaluate
/// <see cref="ConditionGroup"/> expressions on each candidate definition.
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
/// Routing eligibility requires:
/// <list type="bullet">
///   <item><see cref="WorkflowDefinition.IsActive"/> is true.</item>
///   <item>Current UTC time falls within <see cref="WorkflowDefinition.EffectiveFrom"/>/<see cref="WorkflowDefinition.EffectiveTo"/> window.</item>
///   <item>The expression evaluates to true against the supplied context.</item>
/// </list>
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

        var now = DateTime.UtcNow;

        return candidates
            .Where(d => d.IsActive
                && d.EntityType == request.EntityType
                && (d.EffectiveFrom == null || d.EffectiveFrom.Value <= now)
                && (d.EffectiveTo == null || d.EffectiveTo.Value >= now)
                && d.Matches(request.Context))
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Name, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
