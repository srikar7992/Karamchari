// -----------------------------------------------------------------------
// <copyright file="ShiftAssignmentCache.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Lightweight cache of roster shift assignments written by ScheduledShiftCreatedConsumer.
/// AttendanceRecord is created lazily on first punch (or by finalization job for no-shows),
/// avoiding 3M+ pre-generated Pending records for large tenants with 30-day rosters.
/// </summary>
public sealed class ShiftAssignmentCache
{
    private ShiftAssignmentCache() { }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid ShiftId { get; private set; }
    public Guid ShiftAssignmentId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset ExpectedStart { get; private set; }
    public DateTimeOffset ExpectedEnd { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ShiftAssignmentCache Create(
        string tenantId,
        Guid employeeId,
        Guid shiftId,
        Guid shiftAssignmentId,
        DateOnly workDate,
        DateTimeOffset expectedStart,
        DateTimeOffset expectedEnd)
    {
        return new ShiftAssignmentCache
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ShiftId = shiftId,
            ShiftAssignmentId = shiftAssignmentId,
            WorkDate = workDate,
            ExpectedStart = expectedStart,
            ExpectedEnd = expectedEnd,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
