// -----------------------------------------------------------------------
// <copyright file="ShiftUnassignedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Rostering.Events;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Invalidates ShiftAssignmentCache when a shift is unassigned from an employee.
/// Without this, a removed shift stays in cache → ShiftResolver can match the stale entry
/// → AttendanceRecord gets created for a shift the employee is no longer assigned to (phantom attendance).
///
/// Also handles shift reassignment (reassign = unassign old + assign new, two events).
/// ShiftSwapApproved is handled by the same mechanism when both parties' assignments are republished.
/// </summary>
public sealed class ShiftUnassignedConsumer(TimeAttendanceDbContext db, ILogger<ShiftUnassignedConsumer> logger) : IConsumer<ShiftUnassigned>
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly ILogger<ShiftUnassignedConsumer> _logger = logger;

    public async Task Consume(ConsumeContext<ShiftUnassigned> context)
    {
        ShiftUnassigned msg = context.Message;
        CancellationToken ct = context.CancellationToken;

        // Delete cache entry for this specific assignment.
        // Use ShiftAssignmentId for precision — one employee can have multiple shifts on the same day.
        ShiftAssignmentCache? cacheEntry = await _db.ShiftAssignmentCache
            .FirstOrDefaultAsync(c =>
                c.TenantId == msg.TenantId &&
                c.ShiftAssignmentId == msg.ShiftAssignmentId,
                ct);

        if (cacheEntry is null)
        {
            _logger.LogDebug(
                "ShiftUnassigned: no cache entry for assignment {ShiftAssignmentId} (already absent or never cached).",
                msg.ShiftAssignmentId);
            return;
        }

        _db.ShiftAssignmentCache.Remove(cacheEntry);

        // If AttendanceRecord was lazily created but employee hasn't punched yet (still Pending),
        // log a warning. The finalization job will sweep it as a no-show unless the employee punches
        // before shift end — but that won't happen since the shift is now unassigned.
        // HR/payroll teams should be aware that records in Pending state for removed shifts need review.
        AttendanceRecord? orphanRecord = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r =>
                r.TenantId == msg.TenantId &&
                r.ShiftAssignmentId == msg.ShiftAssignmentId &&
                r.Status == Domain.Attendance.AttendanceStatus.Pending,
                ct);

        if (orphanRecord is not null)
        {
            _logger.LogWarning(
                "ShiftUnassigned: Pending AttendanceRecord {RecordId} exists for removed assignment {ShiftAssignmentId}. " +
                "Employee {EmployeeId} had not yet punched. Record will be swept as no-show unless manually corrected.",
                orphanRecord.Id, msg.ShiftAssignmentId, msg.EmployeeId);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ShiftUnassigned: cache entry removed for employee {EmployeeId}, assignment {ShiftAssignmentId}.",
            msg.EmployeeId, msg.ShiftAssignmentId);
    }
}
