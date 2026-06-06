using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Severity classification for a hotspot aggregate.
/// </summary>
public enum HotspotSeverity
{
    /// <summary>Mean score below 40 — no concerning pattern.</summary>
    Normal,
    /// <summary>Mean score 40–64 — monitor closely.</summary>
    Elevated,
    /// <summary>Mean score 65+ — executive-level concern.</summary>
    Critical
}

/// <summary>
/// Statistical aggregate of burnout and attrition scores for an org scope.
/// Enables "executive hotspot" view: which sites/teams are outliers?
///
/// ScopeKey format:
///   "tenant:{tenantId}"        — whole-tenant aggregate
///   "site:{siteCode}"          — single site
///   "team:{teamId}"            — single team
///
/// Upserted on every nightly recompute. Maps to Intel_WorkforceHotspots.
/// </summary>
public sealed class WorkforceHotspot : ITenantOwned
{
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public string ScopeKey { get; private set; } = string.Empty;

    public int TotalEmployees { get; private set; }

    public decimal MeanBurnoutScore { get; private set; }
    public decimal MeanAttritionScore { get; private set; }
    public decimal BurnoutStdDev { get; private set; }
    public decimal AttritionStdDev { get; private set; }

    public int HighRiskBurnoutCount { get; private set; }
    public int CriticalRiskBurnoutCount { get; private set; }
    public int HighRiskAttritionCount { get; private set; }
    public int CriticalRiskAttritionCount { get; private set; }

    /// <summary>Percentage of employees at High or Critical burnout risk.</summary>
    public decimal HighCriticalBurnoutPct { get; private set; }

    /// <summary>Percentage of employees at High or Critical attrition risk.</summary>
    public decimal HighCriticalAttritionPct { get; private set; }

    public HotspotSeverity BurnoutSeverity { get; private set; }
    public HotspotSeverity AttritionSeverity { get; private set; }

    public DateTime ComputedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private WorkforceHotspot() { }

    public static WorkforceHotspot Create(
        string tenantId,
        string scopeKey,
        int totalEmployees,
        decimal meanBurnout,
        decimal meanAttrition,
        decimal burnoutStdDev,
        decimal attritionStdDev,
        int highBurnout,
        int criticalBurnout,
        int highAttrition,
        int criticalAttrition)
    {
        var highCriticalBurnoutPct = totalEmployees > 0
            ? Math.Round((highBurnout + criticalBurnout) * 100m / totalEmployees, 1)
            : 0m;
        var highCriticalAttritionPct = totalEmployees > 0
            ? Math.Round((highAttrition + criticalAttrition) * 100m / totalEmployees, 1)
            : 0m;

        return new WorkforceHotspot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            TotalEmployees = totalEmployees,
            MeanBurnoutScore = meanBurnout,
            MeanAttritionScore = meanAttrition,
            BurnoutStdDev = burnoutStdDev,
            AttritionStdDev = attritionStdDev,
            HighRiskBurnoutCount = highBurnout,
            CriticalRiskBurnoutCount = criticalBurnout,
            HighRiskAttritionCount = highAttrition,
            CriticalRiskAttritionCount = criticalAttrition,
            HighCriticalBurnoutPct = highCriticalBurnoutPct,
            HighCriticalAttritionPct = highCriticalAttritionPct,
            BurnoutSeverity = ClassifySeverity(meanBurnout),
            AttritionSeverity = ClassifySeverity(meanAttrition),
            ComputedAt = DateTime.UtcNow
        };
    }

    public void Refresh(
        int totalEmployees,
        decimal meanBurnout,
        decimal meanAttrition,
        decimal burnoutStdDev,
        decimal attritionStdDev,
        int highBurnout,
        int criticalBurnout,
        int highAttrition,
        int criticalAttrition)
    {
        var highCriticalBurnoutPct = totalEmployees > 0
            ? Math.Round((highBurnout + criticalBurnout) * 100m / totalEmployees, 1)
            : 0m;
        var highCriticalAttritionPct = totalEmployees > 0
            ? Math.Round((highAttrition + criticalAttrition) * 100m / totalEmployees, 1)
            : 0m;

        TotalEmployees = totalEmployees;
        MeanBurnoutScore = meanBurnout;
        MeanAttritionScore = meanAttrition;
        BurnoutStdDev = burnoutStdDev;
        AttritionStdDev = attritionStdDev;
        HighRiskBurnoutCount = highBurnout;
        CriticalRiskBurnoutCount = criticalBurnout;
        HighRiskAttritionCount = highAttrition;
        CriticalRiskAttritionCount = criticalAttrition;
        HighCriticalBurnoutPct = highCriticalBurnoutPct;
        HighCriticalAttritionPct = highCriticalAttritionPct;
        BurnoutSeverity = ClassifySeverity(meanBurnout);
        AttritionSeverity = ClassifySeverity(meanAttrition);
        ComputedAt = DateTime.UtcNow;
    }

    private static HotspotSeverity ClassifySeverity(decimal mean) => mean switch
    {
        < 40m => HotspotSeverity.Normal,
        < 65m => HotspotSeverity.Elevated,
        _ => HotspotSeverity.Critical
    };
}
