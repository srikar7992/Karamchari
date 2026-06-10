// -----------------------------------------------------------------------
// <copyright file="CoverageFragilityService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Computes and upserts <see cref="SiteCoverageFragility"/> records for a tenant.
/// Derives fragility entirely from existing Intelligence data (hotspot + dependency scores),
/// so it requires no cross-module signals.
///
/// Runs once per tenant per nightly recompute pass, after hotspot detection completes.
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/>.
/// </summary>
public sealed class CoverageFragilityService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<CoverageFragilityService> _logger;

    public CoverageFragilityService(IntelligenceDbContext db, ILogger<CoverageFragilityService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Recomputes coverage fragility for tenant-wide scope and all known site scopes.
    /// </summary>
    public async Task RecalculateTenantFragilityAsync(string tenantId, CancellationToken ct = default)
    {
        // ── Tenant-wide scope ────────────────────────────────────────────────

        await UpsertFragilityAsync(tenantId, $"tenant:{tenantId}", null, ct);

        // ── Per-site scopes ──────────────────────────────────────────────────

        var siteCodes = await _db.WorkforceEmployeeScopes
            .Where(s => s.TenantId == tenantId && s.SiteCode != null)
            .Select(s => s.SiteCode!)
            .Distinct()
            .ToListAsync(ct);

        foreach (var siteCode in siteCodes)
        {
            await UpsertFragilityAsync(tenantId, $"site:{siteCode}", siteCode, ct);
        }

        _logger.LogInformation(
            "Coverage fragility computed for tenant {TenantId}: tenant-wide + {SiteCount} sites",
            tenantId, siteCodes.Count);
    }

    private async Task UpsertFragilityAsync(
        string tenantId, string scopeKey, string? siteCode, CancellationToken ct)
    {
        // Get employee IDs for this scope
        List<Guid> scopeEmployeeIds;
        if (siteCode == null)
        {
            scopeEmployeeIds = await _db.WorkforceBurnoutScores
                .Where(b => b.TenantId == tenantId)
                .Select(b => b.EmployeeId)
                .ToListAsync(ct);
        }
        else
        {
            scopeEmployeeIds = await _db.WorkforceEmployeeScopes
                .Where(s => s.TenantId == tenantId && s.SiteCode == siteCode)
                .Select(s => s.EmployeeId)
                .ToListAsync(ct);
        }

        if (scopeEmployeeIds.Count == 0)
        {
            _logger.LogDebug("No employees for scope {ScopeKey} — skipping fragility", scopeKey);
            return;
        }

        // HighCriticalBurnoutPct: fraction at High or Critical burnout
        var burnoutScores = await _db.WorkforceBurnoutScores
            .Where(b => b.TenantId == tenantId && scopeEmployeeIds.Contains(b.EmployeeId))
            .Select(b => new { b.RiskLevel })
            .ToListAsync(ct);

        var highCriticalBurnoutCount = burnoutScores
            .Count(b => b.RiskLevel == WorkforceRiskLevel.High || b.RiskLevel == WorkforceRiskLevel.Critical);
        var highCriticalBurnoutPct = burnoutScores.Count > 0
            ? highCriticalBurnoutCount / (decimal)burnoutScores.Count
            : 0m;

        // CriticalShiftConcentrationRatio: fraction of scope with CriticalShiftCoverageRatio > 0.5
        var criticalShiftSignals = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId
                && scopeEmployeeIds.Contains(s.EmployeeId)
                && s.SignalType == WorkforceSignalType.CriticalShiftCoverageRatio)
            .GroupBy(s => s.EmployeeId)
            .Select(g => g.OrderByDescending(s => s.SignalDate).First().Value)
            .ToListAsync(ct);

        var concentratedCount = criticalShiftSignals.Count(v => v > 0.5m);
        var concentrationRatio = scopeEmployeeIds.Count > 0
            ? concentratedCount / (decimal)scopeEmployeeIds.Count
            : 0m;

        // MeanDependencyScore: average EmployeeDependencyScore across scope
        var depScores = await _db.EmployeeDependencyScores
            .Where(d => d.TenantId == tenantId && scopeEmployeeIds.Contains(d.EmployeeId))
            .Select(d => d.Score)
            .ToListAsync(ct);

        var meanDependency = depScores.Count > 0
            ? depScores.Average()
            : 0m;

        var input = new CoverageFragilityInput(
            HighCriticalBurnoutPct: highCriticalBurnoutPct,
            CriticalShiftConcentrationRatio: concentrationRatio,
            MeanDependencyScore: meanDependency,
            ScopeEmployeeCount: scopeEmployeeIds.Count);

        var result = CoverageFragilityCalculator.Calculate(input);

        var existing = await _db.SiteCoverageFragilities
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.ScopeKey == scopeKey, ct);

        if (existing == null)
        {
            _db.SiteCoverageFragilities.Add(SiteCoverageFragility.Create(
                tenantId, scopeKey,
                result.FragilityScore,
                result.BurnoutPressureComponent,
                result.DependencyConcentrationComponent,
                result.CriticalExposureComponent,
                scopeEmployeeIds.Count));
        }
        else
        {
            existing.Refresh(
                result.FragilityScore,
                result.BurnoutPressureComponent,
                result.DependencyConcentrationComponent,
                result.CriticalExposureComponent,
                scopeEmployeeIds.Count);
        }

        await _db.SaveChangesAsync(ct);
    }
}
