using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class KPIResult : Entity<Guid>, ITenantOwned
{
    private KPIResult() { /* EF materialization */ }

    private KPIResult(
        Guid id,
        string tenantId,
        Guid kpiDefinitionId,
        Guid employeeId,
        string periodLabel,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal rawValue,
        decimal normalizedScore,
        Guid idempotencyKey) : base(id)
    {
        TenantId = tenantId;
        KPIDefinitionId = kpiDefinitionId;
        EmployeeId = employeeId;
        PeriodLabel = periodLabel;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RawValue = rawValue;
        NormalizedScore = normalizedScore;
        Status = KPIResultStatus.Computed;
        ComputedOnUtc = DateTimeOffset.UtcNow;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid KPIDefinitionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Human-readable label. Format: "2026-Q1", "2026-03".</summary>
    public string PeriodLabel { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly PeriodStart { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly PeriodEnd { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal RawValue { get; private set; }

    /// <summary>0â€“100 normalized score for cross-KPI comparison.</summary>
    public decimal NormalizedScore { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIResultStatus Status { get; private set; }
    public Guid? OverriddenBy { get; private set; }
    public string? OverrideJustification { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset ComputedOnUtc { get; private set; }

    /// <summary>Prevents duplicate computation. Unique constraint on (TenantId, IdempotencyKey).</summary>
    public Guid IdempotencyKey { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static KPIResult Record(
        string tenantId,
        Guid kpiDefinitionId,
        Guid employeeId,
        string periodLabel,
        DateOnly periodStart,
        DateOnly periodEnd,
        decimal rawValue,
        decimal normalizedScore)
    {
        var key = Guid.NewGuid();
        return new KPIResult(key, tenantId, kpiDefinitionId, employeeId,
            periodLabel, periodStart, periodEnd, rawValue, normalizedScore, key);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Override(decimal newNormalizedScore, Guid overriddenBy, string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        NormalizedScore = newNormalizedScore;
        OverriddenBy = overriddenBy;
        OverrideJustification = justification;
        Status = KPIResultStatus.ManualOverride;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Lock() => Status = KPIResultStatus.Locked;
}
