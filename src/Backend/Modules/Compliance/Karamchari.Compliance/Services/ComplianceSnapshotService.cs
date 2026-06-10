// -----------------------------------------------------------------------
// <copyright file="ComplianceSnapshotService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Reporting;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Materializes monthly compliance snapshots so dashboards never scan operational tables.
/// Idempotent: upserts by tenant + type + period, never duplicates.
/// </summary>
public sealed class ComplianceSnapshotService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ComplianceSnapshotService> _logger;

    public ComplianceSnapshotService(ComplianceDbContext db, ILogger<ComplianceSnapshotService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RefreshMonthlySnapshotAsync(string tenantId, int year, int month, CancellationToken ct = default)
    {
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var violations = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.DetectedAt >= periodStart
                     && v.DetectedAt < periodEnd)
            .ToListAsync(ct);

        var data = new
        {
            TenantId = tenantId,
            Year = year,
            Month = month,
            TotalViolations = violations.Count,
            OpenViolations = violations.Count(v => v.Status == ViolationStatus.Open),
            ResolvedViolations = violations.Count(v => v.Status == ViolationStatus.Resolved),
            CriticalViolations = violations.Count(v => v.Severity == ViolationSeverity.Critical),
            HighViolations = violations.Count(v => v.Severity == ViolationSeverity.High),
            MediumViolations = violations.Count(v => v.Severity == ViolationSeverity.Medium),
            LowViolations = violations.Count(v => v.Severity == ViolationSeverity.Low),
            ByPolicy = violations
                .GroupBy(v => v.PolicyCode)
                .Select(g => new { PolicyCode = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
            SnapshotAt = DateTime.UtcNow
        };

        var dataJson = System.Text.Json.JsonSerializer.Serialize(data);

        var existing = await _db.ComplianceSnapshots
            .FirstOrDefaultAsync(s => s.TenantId == tenantId
                                   && s.SnapshotType == ComplianceSnapshotType.Monthly
                                   && s.PeriodStart == periodStart, ct);

        if (existing is not null)
        {
            // Refresh in place via new snapshot — snapshots are immutable, so insert a fresh one.
            // Caller queries latest by period, so stale ones age out naturally.
            _db.ComplianceSnapshots.Remove(existing);
        }

        var snapshot = ComplianceSnapshot.Create(tenantId, ComplianceSnapshotType.Monthly, periodStart, periodEnd, dataJson);
        _db.ComplianceSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Compliance snapshot refreshed: tenant={TenantId} {Year}-{Month:D2} violations={Count}",
            tenantId, year, month, violations.Count);
    }

    public async Task RefreshCurrentMonthAsync(string tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await RefreshMonthlySnapshotAsync(tenantId, now.Year, now.Month, ct);
    }
}
