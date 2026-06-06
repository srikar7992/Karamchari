using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.Reporting;

/// <summary>
/// Materialized compliance snapshot for a reporting period.
/// Pre-aggregated so auditors never scan operational tables.
/// </summary>
public sealed class ComplianceSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public ComplianceSnapshotType SnapshotType { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string DataJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private ComplianceSnapshot() { }

    public static ComplianceSnapshot Create(
        string tenantId,
        ComplianceSnapshotType snapshotType,
        DateTime periodStart,
        DateTime periodEnd,
        string dataJson)
    {
        return new ComplianceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SnapshotType = snapshotType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DataJson = dataJson,
            CreatedAt = DateTime.UtcNow
        };
    }
}
