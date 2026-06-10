// -----------------------------------------------------------------------
// <copyright file="DemandForecastGenerator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Generates demand forecasts from three sources in priority order:
/// 1. Manual — manager overrides (highest confidence, always preferred)
/// 2. Historical — rolling 12-week same-day-of-week average from WfpAttendanceSnapshots
/// 3. Baseline — current headcount fallback when no historical data exists (Low confidence)
///
/// Seasonal multipliers are applied to the historical model by month.
///
/// Confidence is variance-aware, not just count-based:
///   combined score = dataQuantityWeight * stabilityWeight
///   - dataQuantityWeight: 90+ pts = 1.0, 30+ = 0.67, below 30 = 0.33
///   - stabilityWeight: CV (stddev/mean) below 0.10 = 1.0, below 0.25 = 0.67, 0.25+ = 0.33
///   - combined 0.67+ = High, 0.33+ = Medium, else Low
/// </summary>
public sealed class DemandForecastGenerator
{
    // Monthly seasonal multipliers (index 0 = January).
    private static readonly decimal[] MonthlyMultipliers =
    [
        1.00m, // Jan
        0.95m, // Feb
        1.00m, // Mar
        1.00m, // Apr
        1.02m, // May
        1.00m, // Jun
        0.90m, // Jul
        0.90m, // Aug
        1.05m, // Sep
        1.10m, // Oct
        1.05m, // Nov
        0.85m, // Dec
    ];

    private readonly ForecastingDbContext _db;

    public DemandForecastGenerator(ForecastingDbContext db) => _db = db;

    /// <summary>
    /// Returns (RequiredHeadcount, Source, Confidence) for the given projection + forecast date.
    /// </summary>
    public async Task<(int RequiredHeadcount, ForecastSource Source, ForecastConfidence Confidence)> GenerateAsync(
        string tenantId,
        string locationCode,
        string skillCode,
        DateOnly forecastDate,
        ForecastHorizon horizon,
        WorkforcePlanningProjection projection,
        CancellationToken ct)
    {
        // 1. Manual override — always wins
        var manual = await _db.DemandForecasts
            .Where(d => d.TenantId == tenantId &&
                        d.LocationCode == locationCode &&
                        d.SkillCode == skillCode &&
                        d.Horizon == horizon &&
                        d.Source == ForecastSource.Manual)
            .OrderByDescending(d => d.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (manual != null)
            return (manual.RequiredHeadcount, ForecastSource.Manual, ForecastConfidence.High);

        // 2. Historical model — rolling 12-week same-day-of-week average
        var dayOfWeek = forecastDate.DayOfWeek;
        var lookbackCutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(7 * 16));

        var snapshots = await _db.WfpAttendanceSnapshots
            .Where(s => s.TenantId == tenantId &&
                        s.LocationCode == locationCode &&
                        s.SkillCode == skillCode &&
                        s.Date >= lookbackCutoff)
            .ToListAsync(ct);

        var sameDaySnapshots = snapshots
            .Where(s => s.Date.DayOfWeek == dayOfWeek)
            .OrderByDescending(s => s.Date)
            .Take(12)
            .ToList();

        if (sameDaySnapshots.Count >= 2)
        {
            var expectedValues = sameDaySnapshots.Select(s => (double)s.ExpectedHeadcount).ToList();
            var avgExpected = expectedValues.Average();
            var seasonalMultiplier = MonthlyMultipliers[forecastDate.Month - 1];
            var demand = (int)Math.Ceiling(avgExpected * (double)seasonalMultiplier);

            var confidence = ComputeVarianceAwareConfidence(projection.DataPointsCount, expectedValues);

            return (Math.Max(1, demand), ForecastSource.Historical, confidence);
        }

        // 3. Baseline fallback — current headcount with Low confidence
        return (Math.Max(1, projection.TotalEmployees), ForecastSource.Historical, ForecastConfidence.Low);
    }

    /// <summary>
    /// Combines data quantity and forecast stability into a single confidence rating.
    /// Data quantity alone is misleading: 500 wildly inconsistent points = Low confidence.
    /// Stability alone is insufficient: 3 perfectly consistent points = unreliable basis.
    /// </summary>
    private static ForecastConfidence ComputeVarianceAwareConfidence(
        int totalDataPoints, List<double> windowValues)
    {
        // Data quantity weight
        double quantityWeight = totalDataPoints switch
        {
            >= 90 => 1.0,
            >= 30 => 0.67,
            _ => 0.33,
        };

        // Stability weight via Coefficient of Variation (stddev / mean)
        double stabilityWeight;
        if (windowValues.Count < 2)
        {
            stabilityWeight = 0.33;
        }
        else
        {
            var mean = windowValues.Average();
            if (mean <= 0)
            {
                stabilityWeight = 0.33;
            }
            else
            {
                var variance = windowValues.Average(v => (v - mean) * (v - mean));
                var cv = Math.Sqrt(variance) / mean; // Coefficient of Variation

                stabilityWeight = cv switch
                {
                    < 0.10 => 1.0,   // Very stable — < 10% relative spread
                    < 0.25 => 0.67,  // Moderate variation
                    _ => 0.33,       // High variation — data is inconsistent
                };
            }
        }

        var combined = quantityWeight * stabilityWeight;

        return combined switch
        {
            >= 0.67 => ForecastConfidence.High,
            >= 0.33 => ForecastConfidence.Medium,
            _ => ForecastConfidence.Low,
        };
    }
}
