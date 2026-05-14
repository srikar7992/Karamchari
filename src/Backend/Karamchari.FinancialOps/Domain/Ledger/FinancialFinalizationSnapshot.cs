using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Immutable closure snapshot for a financial period.
/// Marks the point where the ledger is locked and verified for archival and replay.
/// </summary>
public sealed class FinancialFinalizationSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    private FinancialFinalizationSnapshot(
        Guid id,
        string tenantId,
        FinancialOperationalPeriodId periodId,
        decimal finalizedTotal,
        string checksum)
        : base(id)
    {
        TenantId = tenantId;
        PeriodId = periodId;
        FinalizedTotal = finalizedTotal;
        Checksum = checksum;
        FinalizedAt = DateTimeOffset.UtcNow;
    }

    private FinancialFinalizationSnapshot()
    {
        // EF Core
        TenantId = string.Empty;
        PeriodId = null!;
        Checksum = string.Empty;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the operational period.</summary>
    public FinancialOperationalPeriodId PeriodId { get; private set; }

    /// <summary>Gets the total amount finalized in this period.</summary>
    public decimal FinalizedTotal { get; private set; }

    /// <summary>Gets a cryptographic checksum of the ledger entries in this period.</summary>
    public string Checksum { get; private set; }

    /// <summary>Gets the timestamp.</summary>
    public DateTimeOffset FinalizedAt { get; private set; }

    /// <summary>Creates a new finalization snapshot.</summary>
    public static FinancialFinalizationSnapshot Finalize(
        string tenantId,
        FinancialOperationalPeriodId periodId,
        decimal total,
        string checksum)
    {
        return new FinancialFinalizationSnapshot(
            Guid.NewGuid(),
            tenantId,
            periodId,
            total,
            checksum);
    }
}
