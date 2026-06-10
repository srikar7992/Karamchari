// -----------------------------------------------------------------------
// <copyright file="LeaveEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Leaves.Events;

public sealed record LeaveRequestCreated(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    double TotalDays) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveRequestSubmitted(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    double TotalDays) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveRequestApproved(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    double TotalDays) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveRequestRejected(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveRequestCancelled(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId,
    bool WasApproved) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record CompOffEarned(
    Guid EmployeeId,
    string TenantId,
    DateOnly WorkedOnDate,
    decimal DaysEarned) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveBalanceChanged(
    Guid EmployeeId,
    string TenantId,
    Guid PolicyId,
    decimal NewBalance,
    LeaveBalanceEntryType ChangeType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveWithdrawn(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveExpired(
    Guid RequestId,
    string TenantId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeavePolicyAssigned(
    Guid EmployeeId,
    string TenantId,
    Guid LeaveTypeId,
    Guid PolicyId,
    decimal Entitlement,
    DateOnly EffectiveFrom) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeavePolicyChanged(
    Guid PolicyId,
    string TenantId,
    int NewVersion,
    DateOnly EffectiveFrom,
    string ChangedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveAccrued(
    Guid EmployeeId,
    string TenantId,
    Guid PolicyId,
    decimal DaysAccrued,
    DateOnly EffectiveDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveEncashed(
    Guid EmployeeId,
    string TenantId,
    Guid PolicyId,
    decimal DaysEncashed,
    Guid EncashmentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveCarryForwarded(
    Guid EmployeeId,
    string TenantId,
    Guid PolicyId,
    decimal DaysCarried,
    decimal DaysLapsed,
    Guid FromYearId,
    Guid ToYearId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record LeaveBalanceMigrated(
    Guid EmployeeId,
    string TenantId,
    Guid PolicyId,
    decimal MigratedDays,
    string MigrationBatch) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
