// -----------------------------------------------------------------------
// <copyright file="ComplianceTrendService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Scoring;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compliance.Services;

public sealed class ComplianceTrendService
{
    private readonly ComplianceDbContext _db;

    public ComplianceTrendService(ComplianceDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns score trend snapshots for the past N days (default 30).</summary>
    public async Task<IReadOnlyList<ComplianceScoreHistory>> GetScoreTrendAsync(
        string tenantId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _db.ComplianceScoreHistory
            .Where(h => h.TenantId == tenantId && h.ScopeKey == tenantId && h.RecordedAt >= since)
            .OrderBy(h => h.RecordedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns violation velocity: detections per day over N days.
    /// </summary>
    public async Task<ViolationVelocity> GetViolationVelocityAsync(
        string tenantId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var violations = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.DetectedAt >= since)
            .ToListAsync(ct);

        var resolved = violations.Where(v => v.Status == ViolationStatus.Resolved && v.ResolvedAt.HasValue).ToList();
        var avgResolutionDays = resolved.Count > 0
            ? resolved.Average(v => (v.ResolvedAt!.Value - v.DetectedAt).TotalDays)
            : 0;

        return new ViolationVelocity(
            DetectedLast30d: violations.Count(v => v.DetectedAt >= DateTime.UtcNow.AddDays(-30)),
            DetectedLast90d: await _db.PolicyViolations.CountAsync(
                v => v.TenantId == tenantId && v.DetectedAt >= DateTime.UtcNow.AddDays(-90), ct),
            ResolvedLast30d: resolved.Count,
            AvgResolutionDays: Math.Round(avgResolutionDays, 1),
            RepeatOffenderCount: await GetRepeatOffenderCountAsync(tenantId, ct));
    }

    /// <summary>
    /// Returns site-level risk ranking based on open violation counts.
    /// Groups violations by ScopeKey if site-scoped scores exist; falls back to policy code grouping.
    /// </summary>
    public async Task<IReadOnlyList<RiskHeatmapEntry>> GetRiskHeatmapAsync(
        string tenantId, CancellationToken ct = default)
    {
        var violationsByPolicy = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.Status == ViolationStatus.Open)
            .GroupBy(v => v.PolicyCode)
            .Select(g => new RiskHeatmapEntry(
                g.Key,
                g.Count(),
                g.Count(v => v.Severity == ViolationSeverity.Critical),
                g.Count(v => v.Severity == ViolationSeverity.High)))
            .OrderByDescending(x => x.CriticalCount + x.HighCount)
            .ToListAsync(ct);

        return violationsByPolicy;
    }

    private async Task<int> GetRepeatOffenderCountAsync(string tenantId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-90);
        return await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.DetectedAt >= since)
            .GroupBy(v => v.EmployeeId)
            .CountAsync(g => g.Count() >= 2, ct);
    }
}

public sealed record ViolationVelocity(
    int DetectedLast30d,
    int DetectedLast90d,
    int ResolvedLast30d,
    double AvgResolutionDays,
    int RepeatOffenderCount);

public sealed record RiskHeatmapEntry(string Category, int TotalOpen, int CriticalCount, int HighCount);
