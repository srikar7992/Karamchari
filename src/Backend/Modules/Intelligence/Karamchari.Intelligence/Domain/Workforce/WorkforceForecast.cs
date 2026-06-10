// -----------------------------------------------------------------------
// <copyright file="WorkforceForecast.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Linear-extrapolation forecast for a single employee's burnout or attrition score.
/// One row per (employee, scoreType) — upserted on each nightly run.
/// </summary>
public sealed class WorkforceForecast : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>"Burnout" or "Attrition".</summary>
    public string ScoreType { get; private set; } = string.Empty;

    /// <summary>Current score at time of forecast computation.</summary>
    public decimal CurrentScore { get; private set; }

    /// <summary>Projected score in ForecastHorizonDays calendar days.</summary>
    public decimal ProjectedScore { get; private set; }

    /// <summary>How many days ahead the projection covers.</summary>
    public int ForecastHorizonDays { get; private set; }

    /// <summary>Rate of change (points per day). Positive = worsening.</summary>
    public decimal PointsPerDay { get; private set; }

    /// <summary>Estimated days until Critical threshold (score ≥ 80). Null if already Critical or improving.</summary>
    public int? DaysUntilCritical { get; private set; }

    public ScoreTrend Trend { get; private set; }

    /// <summary>Number of historical data points used to compute the slope.</summary>
    public int DataPointsUsed { get; private set; }

    public WorkforceScoreConfidence Confidence { get; private set; }

    public DateTime ComputedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private WorkforceForecast() { }

    public static WorkforceForecast Create(
        string tenantId,
        Guid employeeId,
        string scoreType,
        decimal currentScore,
        decimal projectedScore,
        int forecastHorizonDays,
        decimal pointsPerDay,
        int? daysUntilCritical,
        ScoreTrend trend,
        int dataPointsUsed,
        WorkforceScoreConfidence confidence)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ScoreType = scoreType,
            CurrentScore = currentScore,
            ProjectedScore = projectedScore,
            ForecastHorizonDays = forecastHorizonDays,
            PointsPerDay = pointsPerDay,
            DaysUntilCritical = daysUntilCritical,
            Trend = trend,
            DataPointsUsed = dataPointsUsed,
            Confidence = confidence,
            ComputedAt = DateTime.UtcNow
        };

    public void Refresh(
        decimal currentScore,
        decimal projectedScore,
        int forecastHorizonDays,
        decimal pointsPerDay,
        int? daysUntilCritical,
        ScoreTrend trend,
        int dataPointsUsed,
        WorkforceScoreConfidence confidence)
    {
        CurrentScore = currentScore;
        ProjectedScore = projectedScore;
        ForecastHorizonDays = forecastHorizonDays;
        PointsPerDay = pointsPerDay;
        DaysUntilCritical = daysUntilCritical;
        Trend = trend;
        DataPointsUsed = dataPointsUsed;
        Confidence = confidence;
        ComputedAt = DateTime.UtcNow;
    }
}
