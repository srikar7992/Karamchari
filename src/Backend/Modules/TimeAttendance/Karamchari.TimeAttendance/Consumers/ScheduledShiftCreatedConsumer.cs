using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Rostering.Events;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Consumes ScheduledShiftCreated events from Rostering (Phase 2).
/// Writes a lightweight ShiftAssignmentCache entry — NOT a full AttendanceRecord.
///
/// Lazy creation rationale: a 100k-employee 30-day roster produces 3M Pending records
/// before anyone works. Instead, AttendanceRecord is created on first punch or by
/// the nightly finalization job for confirmed no-shows.
///
/// Idempotent: if cache entry already exists for (EmployeeId, ShiftId), skip.
/// </summary>
public sealed class ScheduledShiftCreatedConsumer : IConsumer<ScheduledShiftCreated>
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ILogger<ScheduledShiftCreatedConsumer> _logger;

    public ScheduledShiftCreatedConsumer(
        TimeAttendanceDbContext db,
        ILogger<ScheduledShiftCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ScheduledShiftCreated> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var msg = context.Message;

        var exists = await _db.ShiftAssignmentCache
            .AnyAsync(s =>
                s.EmployeeId == msg.EmployeeId &&
                s.ShiftId == msg.RosterShiftId,
            context.CancellationToken);

        if (exists)
        {
            _logger.LogDebug(
                "ShiftAssignmentCache already exists for Employee {EmployeeId} Shift {ShiftId}. Skipping.",
                msg.EmployeeId, msg.RosterShiftId);
            return;
        }

        var expectedStart = new DateTimeOffset(msg.WorkDate.ToDateTime(msg.ExpectedStart), TimeSpan.Zero);
        var expectedEnd = BuildExpectedEnd(msg.WorkDate, msg.ExpectedStart, msg.ExpectedEnd);

        var cache = ShiftAssignmentCache.Create(
            msg.TenantId,
            msg.EmployeeId,
            msg.RosterShiftId,
            msg.ShiftAssignmentId,
            msg.WorkDate,
            expectedStart,
            expectedEnd);

        _db.ShiftAssignmentCache.Add(cache);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug(
            "Cached shift assignment for Employee {EmployeeId} Shift {ShiftId} on {WorkDate}.",
            msg.EmployeeId, msg.RosterShiftId, msg.WorkDate);
    }

    private static DateTimeOffset BuildExpectedEnd(DateOnly workDate, TimeOnly startTime, TimeOnly endTime)
    {
        // Overnight shifts: endTime < startTime means the shift ends the next calendar day.
        var endDate = endTime < startTime ? workDate.AddDays(1) : workDate;
        return new DateTimeOffset(endDate.ToDateTime(endTime), TimeSpan.Zero);
    }
}
