using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// One link in the workforce risk propagation chain.
/// Links are ordered by typical causal sequence:
/// CoverageShortage → OvertimeIncrease → LeaveAvoidance → ScheduleDegradation → BurnoutRisk → AttritionRisk.
/// </summary>
public enum CausalChainLink
{
    /// <summary>Employee covers a disproportionate share of critical shifts — upstream driver.</summary>
    CoverageShortage,

    /// <summary>Overtime hours elevated — typically caused by coverage gaps.</summary>
    OvertimeIncrease,

    /// <summary>Leave avoidance pattern detected — employee not taking entitled leave.</summary>
    LeaveAvoidance,

    /// <summary>Schedule quality degraded — night/morning transitions, split shifts, etc.</summary>
    ScheduleDegradation,

    /// <summary>Burnout score crossed High/Critical threshold.</summary>
    BurnoutRisk,

    /// <summary>Attrition score crossed High/Critical threshold.</summary>
    AttritionRisk
}

/// <summary>
/// Overall severity of the causal chain based on count of active links.
/// </summary>
public enum CausalChainSeverity
{
    /// <summary>No active links.</summary>
    None,

    /// <summary>1–2 active links — early warning.</summary>
    Mild,

    /// <summary>3–4 active links — intervention recommended.</summary>
    Moderate,

    /// <summary>5–6 active links — full propagation chain, urgent action needed.</summary>
    Severe
}

/// <summary>
/// Per-employee active causal chain snapshot.
/// Identifies which links in the Coverage → OT → Leave → Schedule → Burnout → Attrition
/// propagation chain are currently active for this employee.
///
/// Upserted nightly. Maps to Intel_CausalChains.
/// </summary>
public sealed class EmployeeCausalChain : ITenantOwned
{
    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Employee this chain record belongs to.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>JSON-serialised ordered list of active <see cref="CausalChainLink"/> values.</summary>
    public string ActiveLinksJson { get; private set; } = "[]";

    /// <summary>Number of currently active chain links.</summary>
    public int ActiveLinkCount { get; private set; }

    /// <summary>Severity derived from ActiveLinkCount.</summary>
    public CausalChainSeverity ChainSeverity { get; private set; }

    /// <summary>UTC timestamp of last computation.</summary>
    public DateTime ComputedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private EmployeeCausalChain() { }

    /// <summary>Deserialises the active links from JSON.</summary>
    public IReadOnlyList<CausalChainLink> GetActiveLinks() =>
        System.Text.Json.JsonSerializer.Deserialize<List<CausalChainLink>>(ActiveLinksJson) ?? [];

    /// <summary>Creates a new causal chain record.</summary>
    public static EmployeeCausalChain Create(
        string tenantId, Guid employeeId, IReadOnlyList<CausalChainLink> activeLinks)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(activeLinks);

        var count = activeLinks.Count;
        return new EmployeeCausalChain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ActiveLinksJson = System.Text.Json.JsonSerializer.Serialize(activeLinks),
            ActiveLinkCount = count,
            ChainSeverity = ClassifySeverity(count),
            ComputedAt = DateTime.UtcNow
        };
    }

    /// <summary>Refreshes an existing causal chain record in-place.</summary>
    public void Refresh(IReadOnlyList<CausalChainLink> activeLinks)
    {
        ArgumentNullException.ThrowIfNull(activeLinks);

        var count = activeLinks.Count;
        ActiveLinksJson = System.Text.Json.JsonSerializer.Serialize(activeLinks);
        ActiveLinkCount = count;
        ChainSeverity = ClassifySeverity(count);
        ComputedAt = DateTime.UtcNow;
    }

    private static CausalChainSeverity ClassifySeverity(int count) => count switch
    {
        0 => CausalChainSeverity.None,
        <= 2 => CausalChainSeverity.Mild,
        <= 4 => CausalChainSeverity.Moderate,
        _ => CausalChainSeverity.Severe
    };
}
