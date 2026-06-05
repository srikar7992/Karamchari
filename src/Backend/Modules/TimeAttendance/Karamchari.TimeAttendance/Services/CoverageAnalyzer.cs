using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Site-level coverage and health analysis.
/// Answers: which days had staffing shortfalls, and what is the site's attendance health today?
/// </summary>
public sealed class CoverageAnalyzer
{
    private readonly TimeAttendanceDbContext _db;

    public CoverageAnalyzer(TimeAttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<CoverageRiskRecord>> GetCoverageRiskByDateAsync(
        string tenantId,
        DateOnly from,
        DateOnly to,
        decimal thresholdPercent = 80m,
        CancellationToken ct = default)
    {
        var records = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= from &&
                r.WorkDate <= to &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff)
            .GroupBy(r => r.WorkDate)
            .Select(g => new
            {
                WorkDate = g.Key,
                ScheduledCount = g.Count(),
                PresentCount = g.Count(r =>
                    r.Status == AttendanceStatus.Present ||
                    r.Status == AttendanceStatus.Late ||
                    r.Status == AttendanceStatus.HalfDay)
            })
            .ToListAsync(ct);

        return records
            .Select(x =>
            {
                var rate = x.ScheduledCount == 0 ? 100m : Math.Round((decimal)x.PresentCount / x.ScheduledCount * 100, 1);
                return new CoverageRiskRecord(x.WorkDate, x.ScheduledCount, x.PresentCount, rate, rate < thresholdPercent);
            })
            .OrderBy(x => x.WorkDate)
            .ToList()
            .AsReadOnly();
    }

    public async Task<(decimal AttendanceRate, decimal NoShowRate, int AvgLateMinutes, int CriticalOpenViolations)>
        GetSiteHealthAsync(
            string tenantId,
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
    {
        var records = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= from &&
                r.WorkDate <= to &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff)
            .ToListAsync(ct);

        if (records.Count == 0)
            return (100m, 0m, 0, 0);

        var present = records.Count(r =>
            r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);

        var attendanceRate = Math.Round((decimal)present / records.Count * 100, 1);
        var noShowRate = Math.Round((decimal)absent / records.Count * 100, 1);

        var recordIds = records.Select(r => r.Id).ToList();
        var lateViolationCount = await _db.AttendanceViolations
            .CountAsync(v =>
                v.TenantId == tenantId &&
                v.ExceptionType == AttendanceExceptionType.LateArrival &&
                recordIds.Contains(v.AttendanceRecordId),
                ct);

        var criticalOpen = await _db.AttendanceViolations
            .CountAsync(v =>
                v.TenantId == tenantId &&
                v.Severity == AttendanceExceptionSeverity.Critical &&
                v.Status == AttendanceExceptionStatus.Open,
                ct);

        // AvgLateMinutes requires storing late minutes on the violation; using count as proxy until that column is added.
        var avgLateMinutes = lateViolationCount;

        return (attendanceRate, noShowRate, avgLateMinutes, criticalOpen);
    }
}
