// -----------------------------------------------------------------------
// <copyright file="WorkforcePlanningService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Core orchestrator for Phase 5 workforce planning recomputation.
///
/// Called by:
/// - WorkforceRecomputeJob (nightly full rebuild at 01:00 UTC)
/// - RecomputeQueueDrainer (every 30s, drains debounce queue from consumers)
///
/// Phase 5.1 additions:
/// - Cross-skill substitution lookup reduces net hiring need
/// - WfpOvertimePolicy replaces hardcoded 10% overtime assumption
/// - Seasonal attrition blends rolling 3-month rate with same-month historical rate
/// - Composite DayOfWeek+Month absence rates via projection.GetAbsenceRate(date)
/// </summary>
public sealed class WorkforcePlanningService
{
    private static readonly ForecastHorizon[] AllHorizons =
    [
        ForecastHorizon.Days7,
        ForecastHorizon.Days14,
        ForecastHorizon.Days30,
        ForecastHorizon.Days90,
        ForecastHorizon.Days180,
        ForecastHorizon.Days365,
    ];

    private readonly ForecastingDbContext _db;
    private readonly DemandForecastGenerator _demandGenerator;
    private readonly SupplyForecastCalculator _supplyCalculator;
    private readonly ScenarioEngine _scenarioEngine;
    private readonly ILogger<WorkforcePlanningService> _logger;

    public WorkforcePlanningService(
        ForecastingDbContext db,
        DemandForecastGenerator demandGenerator,
        SupplyForecastCalculator supplyCalculator,
        ScenarioEngine scenarioEngine,
        ILogger<WorkforcePlanningService> logger)
    {
        _db = db;
        _demandGenerator = demandGenerator;
        _supplyCalculator = supplyCalculator;
        _scenarioEngine = scenarioEngine;
        _logger = logger;
    }

    /// <summary>
    /// Returns all distinct tenant IDs that have at least one projection row.
    /// Used by <see cref="WorkforceRecomputeJob"/> to drive parallel per-tenant rebuild.
    /// </summary>
    public async Task<List<string>> GetAllTenantIdsAsync(CancellationToken ct)
    {
        return await _db.WorkforcePlanningProjections
            .Select(p => p.TenantId)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Full nightly rebuild: all tenants, all projections, all horizons.
    /// Sequential fallback — parallel dispatch is handled by <see cref="WorkforceRecomputeJob"/>
    /// using one service scope per tenant to avoid cross-thread DbContext access.
    /// </summary>
    public async Task RecomputeAllTenantsAsync(CancellationToken ct)
    {
        var tenantIds = await GetAllTenantIdsAsync(ct);
        foreach (var tenantId in tenantIds)
            await RecomputeForTenantAsync(tenantId, ct);
    }

    public async Task RecomputeForTenantAsync(string tenantId, CancellationToken ct)
    {
        _logger.LogInformation("WFP full recompute start for tenant {TenantId}", tenantId);

        var projections = await _db.WorkforcePlanningProjections
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var proj in projections)
            await RecomputeProjectionInternal(tenantId, proj, today, ct);

        await _db.SaveChangesAsync(ct);

        await _scenarioEngine.RunAllScenariosForTenantAsync(tenantId, ct);

        _logger.LogInformation(
            "WFP full recompute done for tenant {TenantId}: {Count} projections",
            tenantId, projections.Count);
    }

    /// <summary>
    /// Incremental recompute for a single (tenant, location, skill).
    /// Called by RecomputeQueueDrainer after draining debounced event queue.
    /// </summary>
    public async Task RecomputeProjectionAsync(
        string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var proj = await _db.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.LocationCode == locationCode &&
                p.SkillCode == skillCode,
                ct);

        if (proj == null) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await RecomputeProjectionInternal(tenantId, proj, today, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task RecomputeProjectionInternal(
        string tenantId,
        WorkforcePlanningProjection proj,
        DateOnly today,
        CancellationToken ct)
    {
        // Load cross-skill substitutions and overtime policy once per projection
        var substitutions = await _db.WfpSkillSubstitutions
            .Where(s => s.TenantId == tenantId && s.SkillCode == proj.SkillCode && s.IsActive)
            .ToListAsync(ct);

        var overtimePolicy = await _db.WfpOvertimePolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LocationCode == proj.LocationCode, ct);

        var overtimeFactor = overtimePolicy?.OvertimeFactor ?? 0.10m;

        // Phase 5.4: load all supply-side rows once; compute for each horizon in memory.
        // Reduces 7 × 6 = 42 DB round-trips per projection down to 5 queries total.
        var supplyData = await _supplyCalculator.LoadBulkDataAsync(
            tenantId, proj.LocationCode, proj.SkillCode, ct);

        foreach (var horizon in AllHorizons)
        {
            var forecastDate = today.AddDays((int)horizon);

            var (requiredHeadcount, source, confidence) = await _demandGenerator.GenerateAsync(
                tenantId, proj.LocationCode, proj.SkillCode, forecastDate, horizon, proj, ct);

            var supply = _supplyCalculator.CalculateFromBulkData(
                tenantId, proj, forecastDate, horizon, supplyData);

            // Compute cross-skill capacity: substitute employees × CapacityFraction
            var crossSkillCapacity = 0;
            foreach (var sub in substitutions)
            {
                var subProj = await _db.WorkforcePlanningProjections
                    .FirstOrDefaultAsync(p =>
                        p.TenantId == tenantId &&
                        p.LocationCode == proj.LocationCode &&
                        p.SkillCode == sub.SubstituteSkillCode,
                        ct);
                if (subProj != null)
                    crossSkillCapacity += (int)Math.Floor(subProj.TotalEmployees * sub.CapacityFraction);
            }

            var demand = DemandForecast.Create(
                tenantId, forecastDate, proj.LocationCode, proj.SkillCode,
                requiredHeadcount, horizon, source, confidence);

            var risk = CoverageRisk.Compute(
                tenantId, forecastDate, proj.LocationCode, proj.SkillCode,
                demand.RequiredHeadcount, supply.ExpectedHeadcount, horizon);

            var hiringGap = HiringGap.ComputeIfNeeded(
                tenantId, forecastDate, proj.LocationCode, proj.SkillCode,
                demand.RequiredHeadcount, supply.ExpectedHeadcount, horizon,
                crossSkillCapacity, overtimeFactor);

            await PurgeExistingSnapshotsAsync(
                tenantId, proj.LocationCode, proj.SkillCode, forecastDate, horizon, ct);

            _db.DemandForecasts.Add(demand);
            _db.SupplyForecasts.Add(supply);
            _db.CoverageRisks.Add(risk);
            if (hiringGap != null)
                _db.HiringGaps.Add(hiringGap);

            if (risk.RiskLevel >= WfpRiskLevel.High)
            {
                _logger.LogWarning(
                    "WFP [{Tenant}] {Location}/{Skill} horizon={Horizon}d: {RiskLevel} risk — gap={Gap}, netHiring={NetHiring}",
                    tenantId, proj.LocationCode, proj.SkillCode, (int)horizon,
                    risk.RiskLevel, risk.Gap, hiringGap?.NetHiringNeeded ?? 0);
            }
        }
    }

    /// <summary>
    /// Refreshes composite DayOfWeek+Month absence rates on a projection from the last 16 weeks of snapshots.
    /// Composite key "{dow}_{month}" provides seasonal absence awareness (e.g. Monday in December != Monday in July).
    /// </summary>
    public async Task RefreshAbsenceRatesAsync(
        string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var proj = await _db.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.LocationCode == locationCode &&
                p.SkillCode == skillCode,
                ct);

        if (proj == null) return;

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(7 * 16));
        var snapshots = await _db.WfpAttendanceSnapshots
            .Where(s => s.TenantId == tenantId &&
                        s.LocationCode == locationCode &&
                        s.SkillCode == skillCode &&
                        s.Date >= cutoff &&
                        s.ExpectedHeadcount > 0)
            .ToListAsync(ct);

        if (snapshots.Count == 0) return;

        // Composite DayOfWeek+Month rates (e.g. "1_3" = Monday in March)
        var compositeRates = snapshots
            .GroupBy(s => $"{(int)s.Date.DayOfWeek}_{s.Date.Month}")
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var avg = g.Average(s =>
                        (double)(s.ExpectedHeadcount - s.ActualHeadcount) / s.ExpectedHeadcount * 100.0);
                    return Math.Max(0m, (decimal)avg);
                });

        // Day-only fallback rates for days with sparse monthly data
        var dayRates = snapshots
            .GroupBy(s => $"{(int)s.Date.DayOfWeek}")
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var avg = g.Average(s =>
                        (double)(s.ExpectedHeadcount - s.ActualHeadcount) / s.ExpectedHeadcount * 100.0);
                    return Math.Max(0m, (decimal)avg);
                });

        // Merge: composite keys take precedence, day-only fills gaps
        var merged = new Dictionary<string, decimal>(compositeRates);
        foreach (var (k, v) in dayRates)
            merged.TryAdd(k, v);

        var overallRate = (decimal)snapshots.Average(s =>
            (double)(s.ExpectedHeadcount - s.ActualHeadcount) / s.ExpectedHeadcount * 100.0);

        proj.UpdateAbsenteeismRate(Math.Max(0m, overallRate));
        proj.UpdateAbsenceRates(merged);

        var totalExpected = snapshots.Sum(s => (double)s.ExpectedHeadcount);
        if (totalExpected > 0)
        {
            var totalActual = snapshots.Sum(s => (double)s.ActualHeadcount);
            proj.UpdateReliabilityScore((decimal)(totalActual / totalExpected));
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Refreshes the attrition rate for a projection using a seasonal-aware blend:
    /// - Rolling 3-month rate: recent trend (weighted 60%)
    /// - Same-month historical rate: same calendar month across prior years (weighted 40%)
    ///   This captures graduate-season spikes, financial year-end resignations, contract expiry waves.
    /// </summary>
    public async Task RefreshAttritionRateAsync(
        string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var proj = await _db.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.LocationCode == locationCode &&
                p.SkillCode == skillCode,
                ct);

        if (proj == null || proj.TotalEmployees == 0) return;

        var now = DateTime.UtcNow;

        // Rolling 3-month terminations (recent trend)
        var rollingTerminations = 0;
        for (var i = 0; i < 3; i++)
        {
            var target = now.AddMonths(-i);
            var snap = await _db.WfpAttritionSnapshots
                .FirstOrDefaultAsync(a =>
                    a.TenantId == tenantId &&
                    a.LocationCode == locationCode &&
                    a.SkillCode == skillCode &&
                    a.Year == target.Year &&
                    a.Month == target.Month,
                    ct);
            if (snap != null) rollingTerminations += snap.TerminationCount;
        }

        var rollingRate = (decimal)rollingTerminations / proj.TotalEmployees * 100m;

        // Same-month historical rate (seasonal: same month across prior 2 years)
        var sameMonthTerminations = 0;
        var sameMonthYears = 0;
        for (var y = 1; y <= 2; y++)
        {
            var target = now.AddYears(-y);
            var snap = await _db.WfpAttritionSnapshots
                .FirstOrDefaultAsync(a =>
                    a.TenantId == tenantId &&
                    a.LocationCode == locationCode &&
                    a.SkillCode == skillCode &&
                    a.Year == target.Year &&
                    a.Month == now.Month,
                    ct);
            if (snap != null)
            {
                sameMonthTerminations += snap.TerminationCount;
                sameMonthYears++;
            }
        }

        decimal seasonalRate;
        if (sameMonthYears > 0)
        {
            // Annualise same-month average to quarterly rate (month count / 3)
            var avgMonthlyTerminations = (decimal)sameMonthTerminations / sameMonthYears;
            seasonalRate = avgMonthlyTerminations * 3m / proj.TotalEmployees * 100m;
            // Weighted blend: 60% recent, 40% seasonal
            var blendedRate = 0.60m * rollingRate + 0.40m * seasonalRate;
            proj.UpdateAttritionRate(blendedRate);
        }
        else
        {
            // No same-month history — use rolling rate only
            proj.UpdateAttritionRate(rollingRate);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Enqueues a dirty projection key for debounced recompute via RecomputeQueueDrainer.
    ///
    /// Race condition: two concurrent consumers can both pass the AnyAsync check and both attempt
    /// to insert. The unique index on (TenantId, LocationCode, SkillCode) in WFP_RecomputeQueue
    /// is the authoritative guard — a duplicate-key exception is caught and silently discarded
    /// (the key is already queued, so the outcome is correct).
    /// </summary>
    public async Task EnqueueRecomputeAsync(
        string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var exists = await _db.WfpRecomputeEntry.AnyAsync(
            q => q.TenantId == tenantId && q.LocationCode == locationCode && q.SkillCode == skillCode, ct);

        if (exists) return;

        var entry = WfpRecomputeEntry.Enqueue(tenantId, locationCode, skillCode);
        _db.WfpRecomputeEntry.Add(entry);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Concurrent enqueue race: unique constraint rejected the duplicate — already queued.
            _db.Entry(entry).State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Removes stale forecast rows before writing new ones for a given (tenant, location, skill, date, horizon).
    ///
    /// Phase 5.4: uses ExecuteDeleteAsync (EF Core bulk delete) instead of load-then-RemoveRange.
    /// Eliminates 4 SELECT queries per horizon (24 per projection). DELETE executes immediately
    /// against the DB outside the SaveChanges transaction; new rows are inserted by the caller's
    /// subsequent SaveChangesAsync. Manual DemandForecast overrides are preserved.
    /// </summary>
    private async Task PurgeExistingSnapshotsAsync(
        string tenantId,
        string locationCode,
        string skillCode,
        DateOnly forecastDate,
        ForecastHorizon horizon,
        CancellationToken ct)
    {
        await _db.DemandForecasts
            .Where(d => d.TenantId == tenantId && d.LocationCode == locationCode &&
                        d.SkillCode == skillCode && d.ForecastDate == forecastDate &&
                        d.Horizon == horizon && d.Source != ForecastSource.Manual)
            .ExecuteDeleteAsync(ct);

        await _db.SupplyForecasts
            .Where(s => s.TenantId == tenantId && s.LocationCode == locationCode &&
                        s.SkillCode == skillCode && s.ForecastDate == forecastDate &&
                        s.Horizon == horizon)
            .ExecuteDeleteAsync(ct);

        await _db.CoverageRisks
            .Where(r => r.TenantId == tenantId && r.LocationCode == locationCode &&
                        r.SkillCode == skillCode && r.RiskDate == forecastDate &&
                        r.Horizon == horizon)
            .ExecuteDeleteAsync(ct);

        await _db.HiringGaps
            .Where(g => g.TenantId == tenantId && g.LocationCode == locationCode &&
                        g.SkillCode == skillCode && g.PlanningPeriod == forecastDate &&
                        g.Horizon == horizon)
            .ExecuteDeleteAsync(ct);
    }
}
