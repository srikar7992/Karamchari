// -----------------------------------------------------------------------
// <copyright file="ComplianceDashboardQueryService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.LegalHold;
using Karamchari.Compliance.Domain.Reporting;
using Karamchari.Compliance.Domain.Retention;
using Karamchari.Compliance.Domain.Scoring;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Read-only queries for the governance dashboard. No writes.
/// </summary>
public sealed class ComplianceDashboardQueryService
{
    private readonly ComplianceDbContext _db;

    public ComplianceDashboardQueryService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<ComplianceScore?> GetTenantScoreAsync(string tenantId, CancellationToken ct = default)
        => await _db.ComplianceScores
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.ScopeKey == tenantId, ct);

    public async Task<IReadOnlyList<PolicyViolation>> GetOpenViolationsAsync(
        string tenantId, int limit = 50, CancellationToken ct = default)
        => await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.Status == ViolationStatus.Open)
            .OrderByDescending(v => v.DetectedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LegalHold>> GetActiveLegalHoldsAsync(
        string tenantId, CancellationToken ct = default)
        => await _db.LegalHolds
            .Include(h => h.Scopes)
            .Where(h => h.TenantId == tenantId && h.Status == LegalHoldStatus.Active)
            .ToListAsync(ct);

    public async Task<RetentionQueueSummary> GetRetentionQueueSummaryAsync(
        string tenantId, CancellationToken ct = default)
    {
        var scheduled = await _db.RetentionActions
            .Where(a => a.TenantId == tenantId && a.Status == RetentionActionStatus.Scheduled)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new RetentionQueueSummary(
            ArchiveQueue: scheduled.FirstOrDefault(x => x.Action == RetentionActionType.Archive)?.Count ?? 0,
            DeleteQueue: scheduled.FirstOrDefault(x => x.Action == RetentionActionType.Delete)?.Count ?? 0,
            AnonymizeQueue: scheduled.FirstOrDefault(x => x.Action == RetentionActionType.Anonymize)?.Count ?? 0);
    }

    public async Task<ViolationTrendSummary> GetViolationTrendAsync(
        string tenantId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var violations = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.DetectedAt >= since)
            .ToListAsync(ct);

        return new ViolationTrendSummary(
            Total: violations.Count,
            Critical: violations.Count(v => v.Severity == ViolationSeverity.Critical),
            High: violations.Count(v => v.Severity == ViolationSeverity.High),
            Open: violations.Count(v => v.Status == ViolationStatus.Open));
    }
    public async Task<IReadOnlyList<ComplianceScoreHistory>> GetScoreTrendAsync(
        string tenantId, int days = 30, CancellationToken ct = default)
        => await _db.ComplianceScoreHistory
            .Where(h => h.TenantId == tenantId && h.ScopeKey == tenantId && h.RecordedAt >= DateTime.UtcNow.AddDays(-days))
            .OrderBy(h => h.RecordedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PolicyViolation>> GetViolationsInvestigatingAsync(
        string tenantId, CancellationToken ct = default)
        => await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId && v.Status == ViolationStatus.Investigating)
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync(ct);
}

public sealed record RetentionQueueSummary(int ArchiveQueue, int DeleteQueue, int AnonymizeQueue);
public sealed record ViolationTrendSummary(int Total, int Critical, int High, int Open);
