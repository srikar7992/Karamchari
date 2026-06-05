using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Predictive absence risk scoring — answers "who is likely absent on date D?" before the day arrives.
/// This is statistical inference, not ML. Algorithm is transparent and auditable.
///
/// Score = BaseRate × TrendMultiplier, clamped to 0..1.
///
/// BaseRate: historical absence rate for this employee on this day-of-week (90-day window).
/// TrendMultiplier:
///   1.3 if recent reliability (last 4 weeks) has deteriorated >10% vs prior 8-week baseline.
///   0.8 if recent reliability has improved >10% vs baseline.
///   1.0 otherwise (stable).
///
/// RiskLevel thresholds:
///   High   ≥ 0.40
///   Medium ≥ 0.20
///   Low    below 0.20
///
/// Minimum data requirement: 4+ finalized records for the target day-of-week in the 90-day window.
/// Employees with insufficient data are returned with RiskScore=0, RiskLevel=Low, Rationale explains why.
/// </summary>
public sealed class AbsenceRiskPredictor
{
    private const int LookbackDays = 90;
    private const int TrendWeeksTotal = 12;
    private const int RecentWeeks = 4;
    private const int MinRecordsForDow = 4;
    private const decimal DeteriorationThreshold = 0.90m;
    private const decimal ImprovementThreshold = 1.10m;
    private const decimal TrendMultiplierDeteriorating = 1.3m;
    private const decimal TrendMultiplierImproving = 0.8m;
    private const decimal HighRiskThreshold = 0.40m;
    private const decimal MediumRiskThreshold = 0.20m;

    private readonly TimeAttendanceDbContext _db;

    public AbsenceRiskPredictor(TimeAttendanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<AbsenceRiskForecast>> ForecastAsync(
        string tenantId,
        IReadOnlyList<Guid> employeeIds,
        DateOnly targetDate,
        bool includeAll = false,
        CancellationToken ct = default)
    {
        var targetDow = targetDate.DayOfWeek;
        var since90 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-LookbackDays));
        var since12w = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-TrendWeeksTotal * 7));
        var recentCutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-RecentWeeks * 7));

        // Batch load — one query per data set, not per employee
        var employeeIdList = employeeIds.ToList();

        var dowRecords = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                employeeIdList.Contains(r.EmployeeId) &&
                r.WorkDate >= since90 &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff &&
                r.Status != AttendanceStatus.Scheduled)
            .Select(r => new { r.EmployeeId, r.WorkDate, r.Status })
            .ToListAsync(ct);

        var trendRecords = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                employeeIdList.Contains(r.EmployeeId) &&
                r.WorkDate >= since12w &&
                r.Status != AttendanceStatus.Pending &&
                r.Status != AttendanceStatus.OnLeave &&
                r.Status != AttendanceStatus.Holiday &&
                r.Status != AttendanceStatus.WeekOff &&
                r.Status != AttendanceStatus.Scheduled)
            .Select(r => new { r.EmployeeId, r.WorkDate, r.Status })
            .ToListAsync(ct);

        var results = new List<AbsenceRiskForecast>(employeeIds.Count);

        foreach (var employeeId in employeeIds)
        {
            // ── Base rate: historical absence rate for this day-of-week ────────────
            var dowForEmployee = dowRecords
                .Where(r => r.EmployeeId == employeeId && r.WorkDate.DayOfWeek == targetDow)
                .ToList();

            if (dowForEmployee.Count < MinRecordsForDow)
            {
                if (includeAll)
                {
                    results.Add(new AbsenceRiskForecast(
                        employeeId, targetDate, targetDow,
                        RiskScore: 0m,
                        RiskLevel: AbsenceRiskLevel.Low,
                        Rationale: $"Insufficient data: {dowForEmployee.Count} {targetDow} records in last {LookbackDays} days (min {MinRecordsForDow})."));
                }
                continue;
            }

            var absentCount = dowForEmployee.Count(r => r.Status == AttendanceStatus.Absent);
            var baseRate = (decimal)absentCount / dowForEmployee.Count;

            // ── Trend multiplier: recent reliability vs prior baseline ─────────────
            var trendForEmployee = trendRecords
                .Where(r => r.EmployeeId == employeeId)
                .ToList();

            var recentRecords = trendForEmployee.Where(r => r.WorkDate >= recentCutoff).ToList();
            var priorRecords = trendForEmployee.Where(r => r.WorkDate < recentCutoff).ToList();

            decimal trendMultiplier = 1.0m;
            string trendRationale = "stable trend";

            if (recentRecords.Count >= 4 && priorRecords.Count >= 8)
            {
                var recentReliability = (decimal)recentRecords.Count(r =>
                    r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay)
                    / recentRecords.Count;

                var priorReliability = (decimal)priorRecords.Count(r =>
                    r.Status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay)
                    / priorRecords.Count;

                if (priorReliability > 0)
                {
                    var ratio = recentReliability / priorReliability;
                    if (ratio < DeteriorationThreshold)
                    {
                        trendMultiplier = TrendMultiplierDeteriorating;
                        trendRationale = $"deteriorating ({recentReliability:P0} recent vs {priorReliability:P0} baseline)";
                    }
                    else if (ratio > ImprovementThreshold)
                    {
                        trendMultiplier = TrendMultiplierImproving;
                        trendRationale = $"improving ({recentReliability:P0} recent vs {priorReliability:P0} baseline)";
                    }
                }
            }

            // ── Final score ───────────────────────────────────────────────────────
            var score = Math.Min(1m, Math.Round(baseRate * trendMultiplier, 4));
            var level = score >= HighRiskThreshold ? AbsenceRiskLevel.High
                      : score >= MediumRiskThreshold ? AbsenceRiskLevel.Medium
                      : AbsenceRiskLevel.Low;

            if (!includeAll && level == AbsenceRiskLevel.Low)
                continue;

            var rationale =
                $"{targetDow} absence rate {baseRate:P0} over last {LookbackDays} days ({absentCount}/{dowForEmployee.Count}); {trendRationale}.";

            results.Add(new AbsenceRiskForecast(employeeId, targetDate, targetDow, score, level, rationale));
        }

        return results.OrderByDescending(f => f.RiskScore).ToList().AsReadOnly();
    }
}
