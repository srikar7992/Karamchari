using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Represents a point-in-time reconciliation snapshot of the financial ledger.
/// Used to detect drift and validate consistency before finalization.
/// </summary>
public sealed class FinancialReconciliationSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    private FinancialReconciliationSnapshot(
        Guid id,
        string tenantId,
        FinancialOperationalPeriodId periodId,
        decimal expectedTotal,
        decimal actualTotal,
        decimal driftAmount,
        string details)
        : base(id)
    {
        TenantId = tenantId;
        PeriodId = periodId;
        ExpectedTotal = expectedTotal;
        ActualTotal = actualTotal;
        DriftAmount = driftAmount;
        Details = details;
        CreatedAt = DateTimeOffset.UtcNow;
        IsResolved = driftAmount == 0;
    }

    private FinancialReconciliationSnapshot()
    {
        // EF Core
        TenantId = string.Empty;
        PeriodId = null!;
        Details = string.Empty;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the operational period.</summary>
    public FinancialOperationalPeriodId PeriodId { get; private set; }

    /// <summary>Gets the expected total from source documents.</summary>
    public decimal ExpectedTotal { get; private set; }

    /// <summary>Gets the actual total calculated from the ledger.</summary>
    public decimal ActualTotal { get; private set; }

    /// <summary>Gets the drift amount (Expected - Actual).</summary>
    public decimal DriftAmount { get; private set; }

    /// <summary>Gets the reconciliation details.</summary>
    public string Details { get; private set; }

    /// <summary>Gets a value indicating whether the drift is resolved.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Gets the timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a new reconciliation snapshot.</summary>
    public static FinancialReconciliationSnapshot Create(
        string tenantId,
        FinancialOperationalPeriodId periodId,
        decimal expected,
        decimal actual,
        string details)
    {
        return new FinancialReconciliationSnapshot(
            Guid.NewGuid(),
            tenantId,
            periodId,
            expected,
            actual,
            expected - actual,
            details);
    }
}
