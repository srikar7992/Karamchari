using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

public sealed record HabitualLatenessRecord(
    Guid EmployeeId,
    int LateCount,
    int WindowDays,
    DateTimeOffset LastLateAt);

public sealed record CoverageRiskRecord(
    DateOnly WorkDate,
    int ScheduledCount,
    int PresentCount,
    decimal AttendanceRate,
    bool IsBelowThreshold);

public sealed record AttendanceTrendPoint(
    DateOnly WeekStart,
    decimal ReliabilityPercent,
    int PresentCount,
    int AssignedCount);

public sealed record ManagerAlertRecord(
    Guid EmployeeId,
    string AlertType,
    string Detail,
    AttendanceExceptionSeverity Severity,
    DateTimeOffset TriggeredAt);

/// <summary>
/// Facade over the attendance intelligence pipeline.
/// Delegates to focused analyzers — each independently testable and replaceable.
///
/// TrendAnalyzer      — habitual lateness, weekly reliability trend
/// CoverageAnalyzer   — coverage risk by date, site health
/// AbsenceTrendAnalyzer — per-day-of-week absence rate (trend, not ML prediction)
/// AlertGenerator     — open violations + stale regularizations for manager queue
///
/// When any of these grows toward 500 lines, extract further. This facade stays thin.
/// </summary>
public sealed class AttendanceIntelligenceService
{
    private readonly TrendAnalyzer _trends;
    private readonly CoverageAnalyzer _coverage;
    private readonly AbsenceTrendAnalyzer _absenceTrend;
    private readonly AlertGenerator _alerts;

    public AttendanceIntelligenceService(
        TrendAnalyzer trends,
        CoverageAnalyzer coverage,
        AbsenceTrendAnalyzer absenceTrend,
        AlertGenerator alerts)
    {
        _trends = trends;
        _coverage = coverage;
        _absenceTrend = absenceTrend;
        _alerts = alerts;
    }

    public Task<IReadOnlyList<HabitualLatenessRecord>> GetHabitualLatenessAsync(
        string tenantId, int minLateCount = 3, int rollingDays = 30, CancellationToken ct = default)
        => _trends.GetHabitualLatenessAsync(tenantId, minLateCount, rollingDays, ct);

    public Task<IReadOnlyList<AttendanceTrendPoint>> GetAttendanceTrendAsync(
        string tenantId, Guid employeeId, int weeksBack = 12, CancellationToken ct = default)
        => _trends.GetAttendanceTrendAsync(tenantId, employeeId, weeksBack, ct);

    public Task<IReadOnlyList<CoverageRiskRecord>> GetCoverageRiskByDateAsync(
        string tenantId, DateOnly from, DateOnly to, decimal thresholdPercent = 80m, CancellationToken ct = default)
        => _coverage.GetCoverageRiskByDateAsync(tenantId, from, to, thresholdPercent, ct);

    public Task<(decimal AttendanceRate, decimal NoShowRate, int AvgLateMinutes, int CriticalOpenViolations)>
        GetSiteHealthAsync(string tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => _coverage.GetSiteHealthAsync(tenantId, from, to, ct);

    /// <summary>
    /// Historical absence rate by day-of-week. This is a trend observation, not a prediction.
    /// Do not market this as forecasting — forecasting requires statistically validated models.
    /// </summary>
    public Task<IDictionary<DayOfWeek, decimal>> GetAbsenceTrendByDayAsync(
        string tenantId, Guid employeeId, int lookbackDays = 90, CancellationToken ct = default)
        => _absenceTrend.GetAbsenceTrendByDayAsync(tenantId, employeeId, lookbackDays, ct);

    public Task<IReadOnlyList<ManagerAlertRecord>> GetManagerAlertsAsync(
        string tenantId, int maxAlerts = 50, CancellationToken ct = default)
        => _alerts.GetManagerAlertsAsync(tenantId, maxAlerts, ct);
}
