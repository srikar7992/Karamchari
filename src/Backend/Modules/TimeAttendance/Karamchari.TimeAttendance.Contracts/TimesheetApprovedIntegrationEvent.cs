// -----------------------------------------------------------------------
// <copyright file="TimesheetApprovedIntegrationEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Contracts;

/// <summary>
/// Base record for all Karamchari integration events.
/// Consumers must dead-letter any event where ActorId == Guid.Empty.
/// EventId is the idempotency key â€” consumers dedup on this value.
/// </summary>
public abstract record KaramchariIntegrationEvent
{
    /// <summary>Unique identity of this event emission. Used as consumer idempotency key.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Groups all events arising from the same user-initiated operation.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>The EventId of the command that caused this event (for causal tracing).</summary>
    public Guid? CausationId { get; init; }

    /// <summary>UTC time the domain event occurred (not the time the message was published).</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Source of the event (Api, Batch, System).</summary>
    public EventSource Source { get; init; } = EventSource.Api;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum EventSource { Api = 1, Batch = 2, System = 3 }

/// <summary>
/// Integration event published when a timesheet is approved.
/// Carries the full entry set so consumers can operate without re-querying.
/// </summary>
public sealed record TimesheetApprovedIntegrationEvent(
    Guid TimesheetId,
    Guid EmployeeId,
    string TenantId,
    DateOnly WeekStartDate,
    decimal TotalHours,
    IReadOnlyCollection<TimeEntryRecord> Entries,
    bool IsRetroactive,
    Guid ActorId) : KaramchariIntegrationEvent;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record TimeEntryRecord(
    DateOnly Date,
    decimal Hours,
    Guid? ProjectId,
    Guid? TaskId,
    bool IsBillable,
    string? CostCenter,
    string? Tag);

