using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum TrendWindow
{
    Days30 = 30,
    Days90 = 90,
    Days365 = 365,
}

/// <summary>
/// Rolled-up availability trend per workforce group per time window.
/// Computed from WorkforceAvailabilityIndex daily rows.
/// Drives risk detection (Plant below 75%, Department below 80%)
/// and capacity forecast for next week.
/// </summary>
public sealed class WorkforceAvailabilityTrend : ITenantOwned
{
    private WorkforceAvailabilityTrend() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupType { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public TrendWindow Window { get; private set; }
    public DateOnly WindowEndDate { get; private set; }

    // Trend metrics
    public decimal AverageAvailabilityPercent { get; private set; }
    public decimal MinAvailabilityPercent { get; private set; }
    public decimal MaxAvailabilityPercent { get; private set; }
    public int DaysBelowThreshold { get; private set; }
    public decimal ThresholdPercent { get; private set; }
    public bool IsAtRisk { get; private set; }

    // Forecast for next 7 days (simple linear extrapolation from 30-day trend)
    public decimal ForecastedAvailabilityNextWeek { get; private set; }
    public bool ForecastBelowThreshold { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public static WorkforceAvailabilityTrend Compute(
        string tenantId,
        Guid groupId,
        string groupType,
        string groupName,
        TrendWindow window,
        DateOnly windowEndDate,
        IReadOnlyList<decimal> dailyAvailabilityPercents,
        decimal thresholdPercent = 70m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (dailyAvailabilityPercents.Count == 0)
        {
            return new WorkforceAvailabilityTrend
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                GroupId = groupId,
                GroupType = groupType,
                GroupName = groupName,
                Window = window,
                WindowEndDate = windowEndDate,
                ThresholdPercent = thresholdPercent,
                ComputedAt = DateTimeOffset.UtcNow,
            };
        }

        decimal avg = dailyAvailabilityPercents.Average();
        decimal min = dailyAvailabilityPercents.Min();
        decimal max = dailyAvailabilityPercents.Max();
        int daysBelowThreshold = dailyAvailabilityPercents.Count(d => d < thresholdPercent);

        // Naive linear forecast: use last 7 days slope to project next 7
        decimal forecastedNext = avg;
        if (dailyAvailabilityPercents.Count >= 14)
        {
            decimal recentAvg = dailyAvailabilityPercents.TakeLast(7).Average();
            decimal priorAvg = dailyAvailabilityPercents.SkipLast(7).TakeLast(7).Average();
            decimal slope = recentAvg - priorAvg;
            forecastedNext = Math.Max(0m, Math.Min(100m, recentAvg + slope));
        }

        return new WorkforceAvailabilityTrend
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GroupId = groupId,
            GroupType = groupType,
            GroupName = groupName,
            Window = window,
            WindowEndDate = windowEndDate,
            AverageAvailabilityPercent = Math.Round((decimal)avg, 1),
            MinAvailabilityPercent = Math.Round(min, 1),
            MaxAvailabilityPercent = Math.Round(max, 1),
            DaysBelowThreshold = daysBelowThreshold,
            ThresholdPercent = thresholdPercent,
            IsAtRisk = avg < thresholdPercent || daysBelowThreshold > (int)window / 10,
            ForecastedAvailabilityNextWeek = Math.Round(forecastedNext, 1),
            ForecastBelowThreshold = forecastedNext < thresholdPercent,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}
