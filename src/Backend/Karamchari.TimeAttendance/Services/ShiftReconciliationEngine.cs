namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.IoT;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Core brain of the IoT Gateway. Processes raw chronological punches, builds work sessions,
/// applies shift rules, and generates AttendanceResult and TimeEntry records.
/// Versioning enables audit-grade replay.
/// </summary>
public sealed class ShiftReconciliationEngine
{
    private readonly TimeAttendanceDbContext _db;
    private readonly ShiftRosteringEngine _rosteringEngine;
    private readonly ILogger<ShiftReconciliationEngine> _logger;
    private const string CurrentVersion = "v1.0";

    public ShiftReconciliationEngine(
        TimeAttendanceDbContext db, 
        ShiftRosteringEngine rosteringEngine,
        ILogger<ShiftReconciliationEngine> logger)
    {
        _db = db;
        _rosteringEngine = rosteringEngine;
        _logger = logger;
    }

    public async Task ProcessDayAsync(
        string tenantId,
        Guid employeeId, 
        DateTime dateUtc, 
        Guid correlationId,
        CancellationToken ct = default)
    {
        var localDate = dateUtc.Date;
        _logger.LogInformation("Starting reconciliation for Employee:{EmployeeId} on {Date} [Correlation:{CorrelationId}]", 
            employeeId, localDate, correlationId);

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var shift = await _rosteringEngine.ResolveShiftAsync(employeeId, dateUtc, ct);
            
            var endBoundary = shift != null && shift.IsNightShift ? localDate.AddDays(2) : localDate.AddDays(1);
            
            var rawPunches = await _db.RawPunches
                .Where(p => p.TenantId == tenantId && p.EmployeeId == employeeId && p.LocalTimestamp >= localDate && p.LocalTimestamp < endBoundary)
                .OrderBy(p => p.TimestampUtc)
                .ToListAsync(ct);

            var cleanPunches = rawPunches
                .GroupBy(p => RoundToMinute(p.TimestampUtc))
                .Select(g => g.First())
                .ToList();

            var sessions = BuildSessions(cleanPunches);

            int workedMinutes = 0;
            int overtimeMinutes = 0;
            bool isLate = false;
            bool isHalfDay = false;
            bool isAbsent = false;
            bool isPresent = false;

            if (shift != null && sessions.Count > 0)
            {
                var localDateOnly = DateOnly.FromDateTime(localDate);
                var shiftStartUtc = localDateOnly.ToDateTime(new TimeOnly(shift.StartTime.Hours, shift.StartTime.Minutes)).ToUniversalTime();
                var shiftEndUtc = shift.IsNightShift
                    ? localDateOnly.AddDays(1).ToDateTime(new TimeOnly(shift.EndTime.Hours, shift.EndTime.Minutes)).ToUniversalTime()
                    : localDateOnly.ToDateTime(new TimeOnly(shift.EndTime.Hours, shift.EndTime.Minutes)).ToUniversalTime();

                var firstPunch = sessions.First().In;
                if (firstPunch > shiftStartUtc.AddMinutes(shift.GraceMinutes))
                {
                    isLate = true;
                }

                foreach (var s in sessions)
                {
                    if (s.Out == null) continue;

                    var overlapStart = s.In > shiftStartUtc ? s.In : shiftStartUtc;
                    var overlapEnd = s.Out.Value < shiftEndUtc ? s.Out.Value : shiftEndUtc;

                    if (overlapEnd > overlapStart)
                    {
                        workedMinutes += (int)(overlapEnd - overlapStart).TotalMinutes;
                    }

                    if (s.Out.Value > shiftEndUtc)
                    {
                        overtimeMinutes += (int)(s.Out.Value - (s.In > shiftEndUtc ? s.In : shiftEndUtc)).TotalMinutes;
                    }
                }

                if (overtimeMinutes < shift.OvertimeThresholdMinutes) overtimeMinutes = 0;

                isHalfDay = workedMinutes < shift.HalfDayThresholdMinutes;
                isPresent = workedMinutes >= shift.HalfDayThresholdMinutes;
            }
            else if (sessions.Count > 0)
            {
                workedMinutes = sessions.Where(s => s.Out != null).Sum(s => (int)(s.Out!.Value - s.In).TotalMinutes);
                overtimeMinutes = workedMinutes;
                isPresent = true;
            }
            else
            {
                var dateOnly = DateOnly.FromDateTime(localDate);
                var onLeave = await _db.LeaveRequests
                    .AnyAsync(r => r.TenantId == tenantId && 
                                 r.EmployeeId == employeeId && 
                                 r.Status == Karamchari.TimeAttendance.Domain.Leaves.LeaveRequestStatus.Approved &&
                                 dateOnly >= r.StartDate && dateOnly <= r.EndDate, ct);

                if (onLeave)
                {
                    isPresent = true;
                    isAbsent = false;
                }
                else
                {
                    isAbsent = true;
                }
            }

            var existingResult = await _db.AttendanceResults
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.EmployeeId == employeeId && r.Date == localDate, ct);

            if (existingResult != null)
            {
                var audit = AttendanceAudit.Create(
                    tenantId, employeeId, localDate,
                    $"Worked:{existingResult.WorkedMinutes},Present:{existingResult.IsPresent}",
                    $"Worked:{workedMinutes},Present:{isPresent}",
                    $"Engine Update:{correlationId}");
                _db.AttendanceAudits.Add(audit);

                existingResult.Update(isPresent, isLate, isHalfDay, isAbsent, workedMinutes, overtimeMinutes, "Engine Generated", CurrentVersion);
            }
            else
            {
                var newResult = AttendanceResult.Create(
                    tenantId, employeeId, localDate, isPresent, isLate, isHalfDay, isAbsent, workedMinutes, overtimeMinutes, "Engine Generated", CurrentVersion);
                _db.AttendanceResults.Add(newResult);
            }

            await UpsertTimeEntryAsync(tenantId, employeeId, localDate, workedMinutes / 60m, ct);

            foreach (var punch in rawPunches)
            {
                punch.MarkProcessed(CurrentVersion);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            
            _logger.LogInformation("Reconciliation completed for Employee:{EmployeeId} on {Date}. Status: {Status}", 
                employeeId, localDate, isPresent ? "Present" : "Absent");
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            _logger.LogWarning("Concurrency conflict detected for Employee:{EmployeeId} on {Date}. Aborting to allow retry.", employeeId, localDate);
            throw;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Reconciliation failed for Employee:{EmployeeId} on {Date}", employeeId, localDate);
            throw;
        }
    }

    private static List<(DateTime In, DateTime? Out)> BuildSessions(List<RawPunch> punches)
    {
        var sessions = new List<(DateTime In, DateTime? Out)>();
        for (int i = 0; i < punches.Count; i += 2)
        {
            var inPunch = punches[i];
            var outPunch = (i + 1 < punches.Count) ? punches[i + 1] : null;
            sessions.Add((inPunch.TimestampUtc, outPunch?.TimestampUtc));
        }
        return sessions;
    }

    private static DateTime RoundToMinute(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
    }

    private async Task UpsertTimeEntryAsync(string tenantId, Guid employeeId, DateTime localDate, decimal hours, CancellationToken ct)
    {
        var localDateOnly = DateOnly.FromDateTime(localDate);
        int diff = (7 + (localDateOnly.DayOfWeek - DayOfWeek.Monday)) % 7;
        var weekStart = localDateOnly.AddDays(-1 * diff);

        var timesheet = await _db.Timesheets
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.EmployeeId == employeeId && t.WeekStartDate == weekStart, ct);

        if (timesheet == null)
        {
            timesheet = Timesheet.Create(employeeId, weekStart);
            // Timesheet.Create should probably set TenantId too, but assuming Domain handle it or we set it here.
            _db.Timesheets.Add(timesheet);
        }

        if (timesheet.Status == TimesheetStatus.Draft)
        {
            var updated = timesheet.Entries
                .Where(e => e.Date != localDateOnly)
                .Append(new TimeEntry { Date = localDateOnly, Hours = hours })
                .ToList();
            timesheet.UpdateEntries(updated);
        }
    }
}
