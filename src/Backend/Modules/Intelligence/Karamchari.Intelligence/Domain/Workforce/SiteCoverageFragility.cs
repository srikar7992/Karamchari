using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Resilience classification for a coverage scope.
/// </summary>
public enum CoverageFragilityLevel
{
    /// <summary>Score below 25 — coverage can absorb multiple absences.</summary>
    Resilient,

    /// <summary>Score 25–49 — one or two absences would stress coverage.</summary>
    Vulnerable,

    /// <summary>Score 50–74 — a single key absence risks coverage collapse.</summary>
    Fragile,

    /// <summary>Score 75+ — coverage failure is imminent under any absence.</summary>
    Critical
}

/// <summary>
/// Site-level or tenant-level coverage fragility aggregate.
/// Answers: "How many absences does it take to collapse coverage?"
///
/// Derived entirely from existing Intelligence data (hotspot + dependency scores),
/// so it needs no cross-module signals.
///
/// ScopeKey format mirrors <see cref="WorkforceHotspot"/>:
///   "tenant:{tenantId}"   — whole-tenant aggregate
///   "site:{siteCode}"     — single site
///   "dept:{deptCode}"     — single department
///
/// Maps to Intel_CoverageFragility.
/// </summary>
public sealed class SiteCoverageFragility : ITenantOwned
{
    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Scope key for the aggregate (matches WorkforceHotspot.ScopeKey format).</summary>
    public string ScopeKey { get; private set; } = string.Empty;

    /// <summary>Fragility score [0–100]. Higher = more fragile.</summary>
    public decimal FragilityScore { get; private set; }

    /// <summary>Resilience classification derived from FragilityScore.</summary>
    public CoverageFragilityLevel FragilityLevel { get; private set; }

    /// <summary>
    /// Estimated days until a coverage failure event at current trajectory.
    /// Null when fragility is low enough to be considered stable.
    /// </summary>
    public int? DaysUntilCoverageFailure { get; private set; }

    /// <summary>
    /// Component: contribution of high-burnout population to fragility (0–25 pts).
    /// High burnout → elevated unplanned absence probability.
    /// </summary>
    public decimal BurnoutPressureComponent { get; private set; }

    /// <summary>
    /// Component: degree to which critical shifts depend on a thin set of employees (0–40 pts).
    /// High concentration → single absence collapses critical shift coverage.
    /// </summary>
    public decimal DependencyConcentrationComponent { get; private set; }

    /// <summary>
    /// Component: average employee dependency risk across the scope (0–35 pts).
    /// High mean dependency → many roles are irreplaceable.
    /// </summary>
    public decimal CriticalExposureComponent { get; private set; }

    /// <summary>Number of employees in the scope at the time of computation.</summary>
    public int ScopeEmployeeCount { get; private set; }

    /// <summary>UTC timestamp of last computation.</summary>
    public DateTime ComputedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private SiteCoverageFragility() { }

    /// <summary>Creates a new fragility record for a scope.</summary>
    public static SiteCoverageFragility Create(
        string tenantId,
        string scopeKey,
        decimal fragilityScore,
        decimal burnoutPressure,
        decimal dependencyConcentration,
        decimal criticalExposure,
        int scopeEmployeeCount)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(scopeKey);

        return new SiteCoverageFragility
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            FragilityScore = fragilityScore,
            FragilityLevel = Classify(fragilityScore),
            DaysUntilCoverageFailure = EstimateDays(fragilityScore),
            BurnoutPressureComponent = burnoutPressure,
            DependencyConcentrationComponent = dependencyConcentration,
            CriticalExposureComponent = criticalExposure,
            ScopeEmployeeCount = scopeEmployeeCount,
            ComputedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refreshes an existing fragility record in-place (upsert pattern).</summary>
    public void Refresh(
        decimal fragilityScore,
        decimal burnoutPressure,
        decimal dependencyConcentration,
        decimal criticalExposure,
        int scopeEmployeeCount)
    {
        FragilityScore = fragilityScore;
        FragilityLevel = Classify(fragilityScore);
        DaysUntilCoverageFailure = EstimateDays(fragilityScore);
        BurnoutPressureComponent = burnoutPressure;
        DependencyConcentrationComponent = dependencyConcentration;
        CriticalExposureComponent = criticalExposure;
        ScopeEmployeeCount = scopeEmployeeCount;
        ComputedAt = DateTime.UtcNow;
    }

    private static CoverageFragilityLevel Classify(decimal score) => score switch
    {
        < 25m => CoverageFragilityLevel.Resilient,
        < 50m => CoverageFragilityLevel.Vulnerable,
        < 75m => CoverageFragilityLevel.Fragile,
        _ => CoverageFragilityLevel.Critical
    };

    private static int? EstimateDays(decimal score) => score switch
    {
        >= 75m => 7,
        >= 50m => 30,
        >= 25m => 90,
        >= 10m => 180,
        _ => null
    };
}
