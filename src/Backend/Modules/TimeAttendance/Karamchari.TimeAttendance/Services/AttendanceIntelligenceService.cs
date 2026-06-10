// -----------------------------------------------------------------------
// <copyright file="AttendanceIntelligenceService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

/// <summary>
/// Predicted absence risk for one employee on one future date.
/// Risk score ∈ [0,1]. Based on historical day-of-week absence rate and recent trend direction.
/// This is statistical inference from existing records, not ML — treat score as a probability estimate.
/// </summary>
public sealed record AbsenceRiskForecast(
    Guid EmployeeId,
    DateOnly TargetDate,
    DayOfWeek DayOfWeek,
    decimal RiskScore,
    AbsenceRiskLevel RiskLevel,
    string Rationale);

public enum AbsenceRiskLevel { Low, Medium, High }

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
public sealed class AttendanceIntelligenceService(
    TrendAnalyzer trends,
    CoverageAnalyzer coverage,
    AbsenceTrendAnalyzer absenceTrend,
    AlertGenerator alerts,
    AbsenceRiskPredictor riskPredictor)
{
    private readonly TrendAnalyzer _trends = trends;
    private readonly CoverageAnalyzer _coverage = coverage;
    private readonly AbsenceTrendAnalyzer _absenceTrend = absenceTrend;
    private readonly AlertGenerator _alerts = alerts;
    private readonly AbsenceRiskPredictor _riskPredictor = riskPredictor;

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

    /// <summary>
    /// Predicts absence risk for a list of employees on a future date.
    /// Combines historical day-of-week rates with reliability trend direction.
    /// Returns only employees with Medium or High risk unless includeAll is true.
    /// </summary>
    public Task<IReadOnlyList<AbsenceRiskForecast>> GetAbsenceRiskForecastAsync(
        string tenantId, IReadOnlyList<Guid> employeeIds, DateOnly targetDate,
        bool includeAll = false, CancellationToken ct = default)
        => _riskPredictor.ForecastAsync(tenantId, employeeIds, targetDate, includeAll, ct);
}
