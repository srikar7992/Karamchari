using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Immutable point-in-time snapshot materialized at period close.
/// Used for analytics, trend analysis, and anomaly detection.
/// Never updated after creation.
/// </summary>
public sealed class KPISnapshot : Entity<Guid>, ITenantOwned
{
    private KPISnapshot() { /* EF materialization */ }

    private KPISnapshot(
        Guid id,
        string tenantId,
        Guid kpiDefinitionId,
        Guid employeeId,
        string periodLabel,
        decimal value,
        decimal normalizedScore,
        KPIBand band,
        string kpiCode,
        string kpiDisplayName) : base(id)
    {
        TenantId = tenantId;
        KPIDefinitionId = kpiDefinitionId;
        EmployeeId = employeeId;
        PeriodLabel = periodLabel;
        Value = value;
        NormalizedScore = normalizedScore;
        Band = band;
        KPICode = kpiCode;
        KPIDisplayName = kpiDisplayName;
        SnapshotTakenUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid KPIDefinitionId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string PeriodLabel { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public decimal NormalizedScore { get; private set; }
    public KPIBand Band { get; private set; }
    public string KPICode { get; private set; } = string.Empty;
    public string KPIDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset SnapshotTakenUtc { get; private set; }

    public static KPISnapshot Take(
        string tenantId,
        Guid kpiDefinitionId,
        Guid employeeId,
        string periodLabel,
        decimal value,
        decimal normalizedScore,
        KPIBand band,
        string kpiCode,
        string kpiDisplayName)
    {
        return new KPISnapshot(Guid.NewGuid(), tenantId, kpiDefinitionId, employeeId,
            periodLabel, value, normalizedScore, band, kpiCode, kpiDisplayName);
    }
}
