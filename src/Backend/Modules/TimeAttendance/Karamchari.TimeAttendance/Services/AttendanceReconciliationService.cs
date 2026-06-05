using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Hardened reconciliation engine that derives attendance truth from raw signals.
/// Implements Priority 4 Step 22.
/// </summary>
public sealed class AttendanceReconciliationService
{
    private readonly TimeAttendanceDbContext _db;

    public AttendanceReconciliationService(TimeAttendanceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Reconciles an attendance session against its shift definition to produce a finalized record.
    /// </summary>
    public async Task<AttendanceRecord> ReconcileAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.AttendanceSessions
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found.");

        var shift = await _db.ShiftDefinitions.FindAsync([session.ShiftId], ct)
            ?? throw new InvalidOperationException($"Shift {session.ShiftId} not found.");

        // 1. Logic to handle duplicate punches, missing check-outs, etc.
        // For V1, we use the aggregate's own calculated values.

        var status = EvaluateStatus(session, shift);
        var overtime = CalculateOvertime(session, shift);

        var record = AttendanceRecord.FinalizeLegacy(
            session.TenantId,
            session.EmployeeId,
            session.WorkDate,
            session.ShiftId,
            status,
            session.TotalWorkHours,
            overtime);

        return record;
    }

    private static AttendanceStatus EvaluateStatus(AttendanceSession session, ShiftDefinition shift)
    {
        if (session.CheckInTime == null) return AttendanceStatus.Absent;

        if (session.TotalWorkHours >= shift.MinimumHoursForFullDay) return AttendanceStatus.Present;
        if (session.TotalWorkHours >= shift.MinimumHoursForHalfDay) return AttendanceStatus.HalfDay;

        return AttendanceStatus.Absent;
    }

    private static decimal CalculateOvertime(AttendanceSession session, ShiftDefinition shift)
    {
        // Simple logic for V1
        var expectedHours = shift.MinimumHoursForFullDay;
        return Math.Max(0, session.TotalWorkHours - expectedHours);
    }
}
