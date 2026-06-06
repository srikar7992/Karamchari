using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Team/site-level absence contagion detector.
/// Compares current absence rate against the 90-day historical baseline
/// using a z-score test to detect cluster outbreaks.
/// One row per (tenant, siteCode, teamId) — upserted nightly.
/// </summary>
public sealed class AbsenceContagionScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Site code or "*" for tenant-wide aggregate.</summary>
    public string SiteCode { get; private set; } = string.Empty;

    /// <summary>Team identifier. Null = site-level aggregate.</summary>
    public Guid? TeamId { get; private set; }

    /// <summary>Current absence rate [0, 1] over the last 7 days.</summary>
    public decimal CurrentAbsenceRate { get; private set; }

    /// <summary>Historical 90-day baseline absence rate [0, 1].</summary>
    public decimal BaselineAbsenceRate { get; private set; }

    /// <summary>Z-score of the current rate vs baseline. > 1.96 = statistically significant spike.</summary>
    public decimal ZScore { get; private set; }

    public WorkforceRiskLevel RiskLevel { get; private set; }

    public int TeamSize { get; private set; }

    public DateTime CalculatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private AbsenceContagionScore() { }

    public static AbsenceContagionScore Create(
        string tenantId,
        string siteCode,
        Guid? teamId,
        decimal currentRate,
        decimal baselineRate,
        decimal zScore,
        WorkforceRiskLevel riskLevel,
        int teamSize)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SiteCode = siteCode,
            TeamId = teamId,
            CurrentAbsenceRate = currentRate,
            BaselineAbsenceRate = baselineRate,
            ZScore = zScore,
            RiskLevel = riskLevel,
            TeamSize = teamSize,
            CalculatedAt = DateTime.UtcNow
        };

    public void Refresh(
        decimal currentRate,
        decimal baselineRate,
        decimal zScore,
        WorkforceRiskLevel riskLevel,
        int teamSize)
    {
        CurrentAbsenceRate = currentRate;
        BaselineAbsenceRate = baselineRate;
        ZScore = zScore;
        RiskLevel = riskLevel;
        TeamSize = teamSize;
        CalculatedAt = DateTime.UtcNow;
    }
}
