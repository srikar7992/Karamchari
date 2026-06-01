using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;

namespace Karamchari.FinancialOps.Domain.Reimbursements;

/// <summary>
/// Aggregate root for an employee expense claim.
/// Captures the business truth of a reimbursement request and its policy evaluation.
/// Supports deterministic state transitions and operational holds.
/// </summary>
public sealed class ExpenseClaim : AggregateRoot<ExpenseClaimId>, ITenantOwned
{
    private readonly List<FinancialOperationalHold> _holds = [];
    private readonly List<ExpenseReceipt> _receipts = [];

    private ExpenseClaim(
        ExpenseClaimId id,
        string tenantId,
        EmployeeId employeeId,
        ExpenseCategoryId categoryId,
        string claimNumber,
        decimal claimedAmount,
        CurrencyCode currency,
        DateOnly expenseDate,
        int policyVersion,
        TaxTreatment taxTreatment,
        SettlementType preferredSettlementType,
        FinancialReplayMetadata replayMetadata)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        CategoryId = categoryId;
        ClaimNumber = claimNumber;
        ClaimedAmount = claimedAmount;
        Currency = currency;
        ExpenseDate = expenseDate;
        PolicyVersion = policyVersion;
        TaxTreatment = taxTreatment;
        PreferredSettlementType = preferredSettlementType;
        ReplayMetadata = replayMetadata;
        Status = ExpenseClaimStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        ProjectionVersion = 1;
    }

    private ExpenseClaim()
    {
        // EF Core
        TenantId = string.Empty;
        EmployeeId = null!;
        CategoryId = null!;
        ClaimNumber = string.Empty;
        Currency = null!;
        ReplayMetadata = null!;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the employee identifier who submitted the claim.
    /// </summary>
    public EmployeeId EmployeeId { get; private set; }

    /// <summary>
    /// Gets the expense category identifier.
    /// </summary>
    public ExpenseCategoryId CategoryId { get; private set; }

    /// <summary>
    /// Gets the unique business claim number.
    /// </summary>
    public string ClaimNumber { get; private set; }

    /// <summary>
    /// Gets the current operational status of the claim.
    /// </summary>
    public ExpenseClaimStatus Status { get; private set; }

    /// <summary>
    /// Gets the amount claimed for reimbursement.
    /// </summary>
    public decimal ClaimedAmount { get; private set; }

    /// <summary>
    /// Gets the currency of the claim.
    /// </summary>
    public CurrencyCode Currency { get; private set; }

    /// <summary>
    /// Gets the date the expense was incurred.
    /// </summary>
    public DateOnly ExpenseDate { get; private set; }

    /// <summary>
    /// Gets the version of the policy against which this claim was evaluated.
    /// </summary>
    public int PolicyVersion { get; private set; }

    /// <summary>
    /// Gets the tax treatment for this reimbursement.
    /// </summary>
    public TaxTreatment TaxTreatment { get; private set; }

    /// <summary>
    /// Gets the preferred method of settlement for this claim.
    /// </summary>
    public SettlementType PreferredSettlementType { get; private set; }

    /// <summary>
    /// Gets the replay metadata for this claim.
    /// </summary>
    public FinancialReplayMetadata ReplayMetadata { get; private set; }

    /// <summary>
    /// Gets the collection of operational holds currently or historically placed on this claim.
    /// </summary>
    public IReadOnlyList<FinancialOperationalHold> Holds => _holds.AsReadOnly();

    /// <summary>
    /// Gets the collection of receipts attached to this claim.
    /// </summary>
    public IReadOnlyList<ExpenseReceipt> Receipts => _receipts.AsReadOnly();

    /// <summary>
    /// Gets the version of the projection exported to other modules.
    /// </summary>
    public int ProjectionVersion { get; private set; }

    /// <summary>
    /// Gets the timestamp when the claim was created in the system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Submits a new expense claim for review.
    /// </summary>
    public static ExpenseClaim Submit(
        string tenantId,
        EmployeeId employeeId,
        ExpenseCategoryId categoryId,
        string claimNumber,
        decimal claimedAmount,
        CurrencyCode currency,
        DateOnly expenseDate,
        int policyVersion,
        TaxTreatment taxTreatment,
        SettlementType settlementType,
        FinancialReplayMetadata? replayMetadata = null)
    {
        var claim = new ExpenseClaim(
            ExpenseClaimId.New(),
            tenantId,
            employeeId,
            categoryId,
            claimNumber,
            claimedAmount,
            currency,
            expenseDate,
            policyVersion,
            taxTreatment,
            settlementType,
            replayMetadata ?? FinancialReplayMetadata.Empty);

        claim.Status = ExpenseClaimStatus.Submitted;
        return claim;
    }

    /// <summary>
    /// Places an operational hold on the claim, preventing it from proceeding to settlement.
    /// </summary>
    public void PlaceHold(FinancialOperationalHoldType type, string reason, string placedBy)
    {
        if (Status == ExpenseClaimStatus.Settled || Status == ExpenseClaimStatus.Archived)
        {
            throw new InvalidOperationException("Cannot place a hold on a settled or archived claim.");
        }

        _holds.Add(new FinancialOperationalHold(type, reason, placedBy, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Releases an existing operational hold.
    /// </summary>
    public void ReleaseHold(FinancialOperationalHoldType type, string releasedBy)
    {
        var activeHold = _holds.FirstOrDefault(h => h.Type == type && h.IsActive);
        if (activeHold != null)
        {
            _holds.Remove(activeHold);
            _holds.Add(activeHold with
            {
                IsActive = false,
                ReleasedAt = DateTimeOffset.UtcNow,
                ReleasedBy = releasedBy
            });
        }
    }

    /// <summary>
    /// Transitions the claim to Approved status, provided there are no active holds.
    /// </summary>
    public void Approve()
    {
        if (Status != ExpenseClaimStatus.Submitted && Status != ExpenseClaimStatus.UnderReview)
        {
            throw new InvalidOperationException($"Cannot approve claim in state {Status}.");
        }

        if (_holds.Any(h => h.IsActive))
        {
            throw new InvalidOperationException("Cannot approve a claim with active operational holds.");
        }

        Status = ExpenseClaimStatus.Approved;
    }

    /// <summary>
    /// Queues the approved claim for settlement.
    /// </summary>
    public void QueueForSettlement()
    {
        if (Status != ExpenseClaimStatus.Approved)
        {
            throw new InvalidOperationException("Only approved claims can be queued for settlement.");
        }

        Status = ExpenseClaimStatus.QueuedForSettlement;
    }

    /// <summary>
    /// Records the final settlement of the claim.
    /// </summary>
    public void RecordSettlement()
    {
        if (Status != ExpenseClaimStatus.QueuedForSettlement)
        {
            throw new InvalidOperationException("Only queued claims can be marked as settled.");
        }

        Status = ExpenseClaimStatus.Settled;
    }

    /// <summary>
    /// Attaches a receipt to the claim.
    /// </summary>
    public void AddReceipt(ExpenseReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        if (Status != ExpenseClaimStatus.Draft && Status != ExpenseClaimStatus.Submitted)
        {
            throw new InvalidOperationException("Receipts can only be added to claims in Draft or Submitted status.");
        }

        _receipts.Add(receipt);
    }
}
