namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Audit record for a carry-forward transaction at year-end.
/// Exists independently of LeaveLedger so finance can audit cross-year balances.
/// One record per employee per leave type per year-end closing.
/// </summary>
public sealed class LeaveCarryForward : Entity<Guid>, ITenantOwned
{
    private LeaveCarryForward(Guid id, Guid employeeId, Guid policyId,
        Guid fromYearId, Guid toYearId, decimal eligibleDays,
        decimal carriedDays, decimal lapsedDays, string processedBy) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        FromYearId = fromYearId;
        ToYearId = toYearId;
        EligibleDays = eligibleDays;
        CarriedDays = carriedDays;
        LapsedDays = lapsedDays;
        ProcessedBy = processedBy;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    private LeaveCarryForward()
    {
        TenantId = string.Empty;
        ProcessedBy = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }

    /// <summary>Leave year the balance is carried from.</summary>
    public Guid FromYearId { get; private set; }

    /// <summary>Leave year the balance is carried into.</summary>
    public Guid ToYearId { get; private set; }

    /// <summary>Total balance available at year-end before carry-forward cap applied.</summary>
    public decimal EligibleDays { get; private set; }

    /// <summary>Days actually carried forward (capped by MaximumCarryForward policy).</summary>
    public decimal CarriedDays { get; private set; }

    /// <summary>Days lapsed due to cap or policy expiry.</summary>
    public decimal LapsedDays { get; private set; }

    public string ProcessedBy { get; private set; }
    public DateTimeOffset ProcessedAtUtc { get; private set; }

    /// <summary>Reference to the ledger entry created for this carry-forward.</summary>
    public Guid? LedgerEntryId { get; private set; }

    public static LeaveCarryForward Create(Guid employeeId, Guid policyId,
        Guid fromYearId, Guid toYearId, decimal eligibleDays,
        decimal maxCarryForward, string processedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processedBy);
        ArgumentOutOfRangeException.ThrowIfNegative(eligibleDays);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCarryForward);

        var carried = Math.Min(eligibleDays, maxCarryForward);
        var lapsed = eligibleDays - carried;

        return new LeaveCarryForward(Guid.NewGuid(), employeeId, policyId,
            fromYearId, toYearId, eligibleDays, carried, lapsed, processedBy);
    }

    public void LinkLedgerEntry(Guid ledgerEntryId) => LedgerEntryId = ledgerEntryId;
}
