// -----------------------------------------------------------------------
// <copyright file="TrendAnalyzer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Employee-level attendance trend analysis.
/// Answers: who is habitually late, and is this employee's reliability improving or deteriorating?
/// </summary>
public sealed class TrendAnalyzer(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<IReadOnlyList<HabitualLatenessRecord>> GetHabitualLatenessAsync(
        string tenantId,
        int minLateCount = 3,
        int rollingDays = 30,
        CancellationToken ct = default)
    {
        DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-rollingDays);

        var results = await _db.AttendanceViolations
            .Where(v =>
                v.TenantId == tenantId &&
                v.ExceptionType == AttendanceExceptionType.LateArrival &&
                v.CreatedAt >= since)
            .GroupBy(v => v.EmployeeId)
            .Select(g => new
            {
                EmployeeId = g.Key,
                LateCount = g.Count(),
                LastLateAt = g.Max(v => v.CreatedAt)
            })
            .Where(x => x.LateCount >= minLateCount)
            .OrderByDescending(x => x.LateCount)
            .ToListAsync(ct);

        return results
            .Select(x => new HabitualLatenessRecord(x.EmployeeId, x.LateCount, rollingDays, x.LastLateAt))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<AttendanceTrendPoint>> GetAttendanceTrendAsync(
        string tenantId,
        Guid employeeId,
        int weeksBack = 12,
        CancellationToken ct = default)
    {
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-weeksBack * 7));

        List<AttendanceRecord> records = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.EmployeeId == employeeId &&
                r.WorkDate >= since &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff)
            .ToListAsync(ct);

        return records
            .GroupBy(r => GetWeekStart(r.WorkDate))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                int assigned = g.Count();
                int present = g.Count(r =>
                    r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay);
                decimal reliability = assigned == 0 ? 100m : Math.Round((decimal)present / assigned * 100, 1);
                return new AttendanceTrendPoint(g.Key, reliability, present, assigned);
            })
            .ToList()
            .AsReadOnly();
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        int daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-daysToMonday);
    }
}
