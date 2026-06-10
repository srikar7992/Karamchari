// -----------------------------------------------------------------------
// <copyright file="ComplianceScoringService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Scoring;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Computes and upserts the compliance score for a tenant scope.
/// Formula: 100 - ViolationPenalty - OvertimePenalty - AuditFindingPenalty - PolicyExceptionPenalty
/// Penalty derivation:
///   Critical violation = 15 points, High = 8, Medium = 3, Low = 1
///   Penalty capped at 60 points total from violations.
/// </summary>
public sealed class ComplianceScoringService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ComplianceScoringService> _logger;

    public ComplianceScoringService(ComplianceDbContext db, ILogger<ComplianceScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecomputeTenantScoreAsync(string tenantId, CancellationToken ct = default)
    {
        var scopeKey = tenantId;
        await RecomputeScopeAsync(tenantId, scopeKey, ct);
    }

    public async Task RecomputeScopeAsync(string tenantId, string scopeKey, CancellationToken ct = default)
    {
        var openViolations = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && (v.Status == ViolationStatus.Open || v.Status == ViolationStatus.Investigating))
            .ToListAsync(ct);

        decimal violationPenalty = 0m;
        decimal overtimePenalty = 0m;
        int criticalCount = 0;

        foreach (var v in openViolations)
        {
            var pts = v.Severity switch
            {
                ViolationSeverity.Critical => 15m,
                ViolationSeverity.High => 8m,
                ViolationSeverity.Medium => 3m,
                _ => 1m
            };

            if (v.PolicyCode.StartsWith("POL-OT", StringComparison.Ordinal))
                overtimePenalty += pts;
            else
                violationPenalty += pts;

            if (v.Severity == ViolationSeverity.Critical) criticalCount++;
        }

        // Caps
        violationPenalty = Math.Min(violationPenalty, 40m);
        overtimePenalty = Math.Min(overtimePenalty, 20m);

        var existing = await _db.ComplianceScores
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.ScopeKey == scopeKey, ct);

        if (existing is null)
        {
            var created = ComplianceScore.Create(
                tenantId, scopeKey,
                violationPenalty, overtimePenalty,
                auditFindingPenalty: 0m, policyExceptionPenalty: 0m,
                openViolations.Count, criticalCount);
            _db.ComplianceScores.Add(created);
        }
        else
        {
            existing.Refresh(
                violationPenalty, overtimePenalty,
                auditFindingPenalty: 0m, policyExceptionPenalty: 0m,
                openViolations.Count, criticalCount);
        }

        await _db.SaveChangesAsync(ct);

        // Record score history snapshot for trend analytics
        var currentScore = existing ?? await _db.ComplianceScores
            .FirstAsync(s => s.TenantId == tenantId && s.ScopeKey == scopeKey, ct);
        var historyEntry = ComplianceScoreHistory.Record(
            tenantId, scopeKey,
            currentScore.Score, currentScore.Level,
            currentScore.OpenViolationCount, currentScore.CriticalViolationCount);
        _db.ComplianceScoreHistory.Add(historyEntry);
        await _db.SaveChangesAsync(ct);

        _logger.LogDebug("Compliance score recomputed for scope {Scope}: violations={V}, ot={O}",
            scopeKey, violationPenalty, overtimePenalty);
    }
}
