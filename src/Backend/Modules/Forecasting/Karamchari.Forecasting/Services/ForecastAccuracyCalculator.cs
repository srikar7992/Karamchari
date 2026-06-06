using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Records forecast accuracy by comparing yesterday's demand forecast against actual attendance.
/// Called by WorkforceRecomputeJob after each nightly rebuild.
///
/// For each (tenant, location, skill) that has an attendance snapshot for yesterday,
/// finds the matching DemandForecast for that date and records:
/// - AbsoluteError = |actual - predicted|
/// - AbsolutePercentError (MAPE component) = AbsoluteError / actual × 100
///
/// Accumulating WfpForecastAccuracy rows over time answers:
/// "Are my forecasts improving or degrading?"
///
/// Query example (SQL): SELECT avg(AbsolutePercentError) FROM WFP_ForecastAccuracy
///   WHERE TenantId = @t AND Horizon = 7 AND ForecastDate > dateadd(month,-3,getutcdate())
/// </summary>
public sealed class ForecastAccuracyCalculator
{
    private readonly ForecastingDbContext _db;
    private readonly ILogger<ForecastAccuracyCalculator> _logger;

    public ForecastAccuracyCalculator(ForecastingDbContext db, ILogger<ForecastAccuracyCalculator> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Records accuracy for yesterday across all tenants. Called once per nightly run.</summary>
    public async Task RecordYesterdayAccuracyAsync(CancellationToken ct)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        // Find all attendance snapshots for yesterday (these are our "actuals")
        var actuals = await _db.WfpAttendanceSnapshots
            .Where(s => s.Date == yesterday && s.ActualHeadcount > 0)
            .ToListAsync(ct);

        if (actuals.Count == 0) return;

        var recorded = 0;
        foreach (var actual in actuals)
        {
            // Skip if we already have an accuracy record for this combination
            var alreadyExists = await _db.WfpForecastAccuracies.AnyAsync(a =>
                a.TenantId == actual.TenantId &&
                a.LocationCode == actual.LocationCode &&
                a.SkillCode == actual.SkillCode &&
                a.ForecastDate == yesterday,
                ct);
            if (alreadyExists) continue;

            // Find the most recent non-manual DemandForecast for yesterday
            var predicted = await _db.DemandForecasts
                .Where(d =>
                    d.TenantId == actual.TenantId &&
                    d.LocationCode == actual.LocationCode &&
                    d.SkillCode == actual.SkillCode &&
                    d.ForecastDate == yesterday &&
                    d.Source != ForecastSource.Manual)
                .OrderByDescending(d => d.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            if (predicted == null) continue;

            _db.WfpForecastAccuracies.Add(WfpForecastAccuracy.Record(
                actual.TenantId,
                actual.LocationCode,
                actual.SkillCode,
                yesterday,
                predicted.Horizon,
                predicted.RequiredHeadcount,
                actual.ActualHeadcount));

            recorded++;
        }

        if (recorded > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "WFP accuracy: recorded {Count} observations for {Date}", recorded, yesterday);
        }
    }
}
