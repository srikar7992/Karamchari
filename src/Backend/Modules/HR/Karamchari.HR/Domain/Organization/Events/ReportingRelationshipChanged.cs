using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

/// <summary>
/// Domain event raised when a reporting relationship changes, detailing the report, 
/// the transition from an old manager position to a new manager position, and its effective date.
/// </summary>
/// <param name="RelationshipId">The unique identifier of the reporting relationship.</param>
/// <param name="TenantId">The tenant identifier associated with the change.</param>
/// <param name="DirectReportPositionId">The position identifier of the employee reporting.</param>
/// <param name="OldManagerPositionId">The previous manager's position identifier, if any.</param>
/// <param name="NewManagerPositionId">The new manager's position identifier, if any.</param>
/// <param name="EffectiveFrom">The timestamp when this reporting line transition becomes effective.</param>
public sealed record ReportingRelationshipChanged(
    Guid RelationshipId,
    string TenantId,
    Guid DirectReportPositionId,
    Guid? OldManagerPositionId,
    Guid? NewManagerPositionId,
    DateTimeOffset EffectiveFrom) : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this domain event.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when this event occurred in UTC.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

