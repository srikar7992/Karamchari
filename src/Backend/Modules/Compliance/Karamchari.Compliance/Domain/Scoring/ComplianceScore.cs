using Karamchari.Compliance.Contracts;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Scoring;

/// <summary>
/// Latest compliance score for a tenant scope (tenant-wide or per-site).
/// Upserted each time the scoring engine runs.
/// Formula: 100 - ViolationPenalty - OvertimePenalty - AuditFindingPenalty - PolicyExceptionPenalty
/// </summary>
public sealed class ComplianceScore : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Tenant-wide key = tenantId. Site key = "{tenantId}:{siteCode}".</summary>
    public string ScopeKey { get; private set; } = string.Empty;

    public decimal Score { get; private set; }
    public ComplianceLevel Level { get; private set; }
    public decimal ViolationPenalty { get; private set; }
    public decimal OvertimePenalty { get; private set; }
    public decimal AuditFindingPenalty { get; private set; }
    public decimal PolicyExceptionPenalty { get; private set; }
    public int OpenViolationCount { get; private set; }
    public int CriticalViolationCount { get; private set; }
    public DateTime CalculatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private ComplianceScore() { }

    public static ComplianceScore Create(
        string tenantId,
        string scopeKey,
        decimal violationPenalty,
        decimal overtimePenalty,
        decimal auditFindingPenalty,
        decimal policyExceptionPenalty,
        int openViolationCount,
        int criticalViolationCount)
    {
        var score = Math.Max(0m, 100m - violationPenalty - overtimePenalty - auditFindingPenalty - policyExceptionPenalty);
        var level = DeriveLevel(score);

        return new ComplianceScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            Score = score,
            Level = level,
            ViolationPenalty = violationPenalty,
            OvertimePenalty = overtimePenalty,
            AuditFindingPenalty = auditFindingPenalty,
            PolicyExceptionPenalty = policyExceptionPenalty,
            OpenViolationCount = openViolationCount,
            CriticalViolationCount = criticalViolationCount,
            CalculatedAt = DateTime.UtcNow
        };
    }

    public void Refresh(
        decimal violationPenalty,
        decimal overtimePenalty,
        decimal auditFindingPenalty,
        decimal policyExceptionPenalty,
        int openViolationCount,
        int criticalViolationCount)
    {
        var oldScore = Score;
        var newScore = Math.Max(0m, 100m - violationPenalty - overtimePenalty - auditFindingPenalty - policyExceptionPenalty);
        var newLevel = DeriveLevel(newScore);

        Score = newScore;
        Level = newLevel;
        ViolationPenalty = violationPenalty;
        OvertimePenalty = overtimePenalty;
        AuditFindingPenalty = auditFindingPenalty;
        PolicyExceptionPenalty = policyExceptionPenalty;
        OpenViolationCount = openViolationCount;
        CriticalViolationCount = criticalViolationCount;
        CalculatedAt = DateTime.UtcNow;

        if (Math.Abs(oldScore - newScore) >= 1m)
        {
            RaiseDomainEvent(new ComplianceScoreChangedEvent(
                Id, TenantId, ScopeKey, oldScore, newScore, newLevel.ToString(), CalculatedAt));
        }
    }

    private static ComplianceLevel DeriveLevel(decimal score) => score switch
    {
        >= 90m => ComplianceLevel.Excellent,
        >= 80m => ComplianceLevel.Good,
        >= 70m => ComplianceLevel.Warning,
        >= 60m => ComplianceLevel.Risk,
        _ => ComplianceLevel.Critical
    };
}
