// -----------------------------------------------------------------------
// <copyright file="LeaveTimelineEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Append-only projection of every domain event that touched a leave request.
/// Rebuilt from domain events; never mutated. Used for dispute resolution and audit.
/// </summary>
public sealed class LeaveTimelineEntry : ITenantOwned
{
    private LeaveTimelineEntry() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public string? Comment { get; private set; }
    /// <summary>JSON snapshot of before/after state diff for the event.</summary>
    public string? Metadata { get; private set; }

    public static LeaveTimelineEntry Record(
        string tenantId,
        Guid leaveRequestId,
        Guid employeeId,
        string eventType,
        string actorId,
        string actorRole,
        DateTimeOffset occurredAt,
        string? comment = null,
        string? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        return new LeaveTimelineEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LeaveRequestId = leaveRequestId,
            EmployeeId = employeeId,
            OccurredAt = occurredAt,
            EventType = eventType,
            ActorId = actorId,
            ActorRole = actorRole,
            Comment = comment,
            Metadata = metadata,
        };
    }
}
