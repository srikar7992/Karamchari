// -----------------------------------------------------------------------
// <copyright file="LeaveForecast.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum ForecastGranularity
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
}

/// <summary>
/// Time-series leave demand forecast using rolling historical averages.
/// No ML required — uses 3-year same-period average leave utilization %.
/// Granularity: Weekly/Monthly/Quarterly per team/department/location.
/// </summary>
public sealed class LeaveForecast : ITenantOwned
{
    private LeaveForecast() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupType { get; private set; } = string.Empty;
    public ForecastGranularity Granularity { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public int HeadcountScheduled { get; private set; }
    public decimal ForecastedLeavePercent { get; private set; }
    public int ForecastedOnLeaveCount { get; private set; }
    public int HistoricalYearsUsed { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static LeaveForecast Compute(
        string tenantId,
        Guid groupId,
        string groupType,
        ForecastGranularity granularity,
        DateOnly periodStart,
        DateOnly periodEnd,
        int headcountScheduled,
        decimal forecastedLeavePercent,
        int historicalYearsUsed,
        decimal confidenceScore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegative(headcountScheduled);

        int forecastedCount = (int)Math.Round(headcountScheduled * forecastedLeavePercent / 100m);

        return new LeaveForecast
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GroupId = groupId,
            GroupType = groupType,
            Granularity = granularity,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            HeadcountScheduled = headcountScheduled,
            ForecastedLeavePercent = forecastedLeavePercent,
            ForecastedOnLeaveCount = forecastedCount,
            HistoricalYearsUsed = historicalYearsUsed,
            ConfidenceScore = confidenceScore,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}
