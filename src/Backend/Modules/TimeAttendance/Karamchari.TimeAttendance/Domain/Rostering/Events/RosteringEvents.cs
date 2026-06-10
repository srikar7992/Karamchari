// -----------------------------------------------------------------------
// <copyright file="RosteringEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Rostering.Events;

public sealed record RosterCreated(
    Guid RosterId,
    string TenantId,
    DateOnly StartDate,
    DateOnly EndDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ShiftAssigned(
    Guid RosterId,
    string TenantId,
    Guid ShiftAssignmentId,
    Guid EmployeeId,
    DateOnly WorkDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ShiftUnassigned(
    Guid RosterId,
    string TenantId,
    Guid ShiftAssignmentId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RosterPublished(
    Guid RosterId,
    string TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    int AssignmentCount,
    DateTimeOffset PublishedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RosterMovedToReview(
    Guid RosterId,
    string TenantId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record CoverageBreachDetected(
    string TenantId,
    Guid RosterId,
    Guid LocationId,
    DateOnly Date,
    int Required,
    int Assigned) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ScheduleAlertDetected(
    string TenantId,
    Guid RosterId,
    Guid EmployeeId,
    ExceptionType ExceptionType,
    ExceptionSeverity Severity,
    string Message) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ShiftSwapRequested(
    Guid SwapId,
    string TenantId,
    Guid RequestedBy,
    Guid RequestedWith) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record ShiftSwapApproved(
    Guid SwapId,
    string TenantId,
    Guid RequestedBy,
    Guid RequestedWith,
    Guid ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised once per active assignment when a roster publishes.
/// Carries all data Attendance (Phase 3) needs to create expected-attendance records.
/// </summary>
public sealed record ScheduledShiftCreated(
    string TenantId,
    Guid RosterId,
    Guid RosterShiftId,
    Guid ShiftAssignmentId,
    Guid EmployeeId,
    DateOnly WorkDate,
    TimeOnly ExpectedStart,
    TimeOnly ExpectedEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
