// -----------------------------------------------------------------------
// <copyright file="ComplianceScoreHistory.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Compliance.Domain.Scoring;

/// <summary>
/// Immutable snapshot of a compliance score at a point in time.
/// Created each time ComplianceScoringService recomputes a scope score.
/// Used for trend analytics (30/90 day charts, velocity tracking).
/// </summary>
public sealed class ComplianceScoreHistory : Entity<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public string ScopeKey { get; private set; } = string.Empty;
    public decimal Score { get; private set; }
    public ComplianceLevel Level { get; private set; }
    public int OpenViolationCount { get; private set; }
    public int CriticalViolationCount { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private ComplianceScoreHistory() { }

    public static ComplianceScoreHistory Record(
        string tenantId,
        string scopeKey,
        decimal score,
        ComplianceLevel level,
        int openViolationCount,
        int criticalViolationCount)
    {
        return new ComplianceScoreHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            Score = score,
            Level = level,
            OpenViolationCount = openViolationCount,
            CriticalViolationCount = criticalViolationCount,
            RecordedAt = DateTime.UtcNow
        };
    }
}
