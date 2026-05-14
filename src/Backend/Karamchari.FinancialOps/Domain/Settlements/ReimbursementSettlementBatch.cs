using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Contracts.Reimbursements;
using Karamchari.FinancialOps.Contracts.Settlements;

namespace Karamchari.FinancialOps.Domain.Settlements;

/// <summary>
/// Aggregate root for a batch of expense claims to be settled together.
/// Orchestrates the execution and reconciliation of payouts, supporting large-scale partitioning.
/// </summary>
public sealed class ReimbursementSettlementBatch : AggregateRoot<SettlementBatchId>, ITenantOwned
{
    private readonly List<ExpenseClaimId> _claimIds = [];

    private ReimbursementSettlementBatch(
        SettlementBatchId id,
        string tenantId,
        FinancialOperationalPeriodId periodId,
        SettlementType type,
        IEnumerable<ExpenseClaimId> claimIds)
        : base(id)
    {
        TenantId = tenantId;
        PeriodId = periodId;
        Type = type;
        Status = SettlementBatchStatus.Draft;
        _claimIds.AddRange(claimIds);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private ReimbursementSettlementBatch()
    {
        // EF Core
        TenantId = string.Empty;
        PeriodId = null!;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the financial operational period this batch belongs to.
    /// </summary>
    public FinancialOperationalPeriodId PeriodId { get; private set; }

    /// <summary>
    /// Gets the settlement method for this batch.
    /// </summary>
    public SettlementType Type { get; private set; }

    /// <summary>
    /// Gets the current operational status of the batch.
    /// </summary>
    public SettlementBatchStatus Status { get; private set; }

    /// <summary>
    /// Gets the collection of claims included in this batch.
    /// </summary>
    public IReadOnlyCollection<ExpenseClaimId> ClaimIds => _claimIds.AsReadOnly();

    /// <summary>
    /// Gets the external reference identifier from the payout provider (e.g., Bank Transfer Ref).
    /// </summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Gets the timestamp when the batch was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the batch execution started.
    /// </summary>
    public DateTimeOffset? ExecutedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the batch was finalized and locked.
    /// </summary>
    public DateTimeOffset? FinalizedAt { get; private set; }

    /// <summary>
    /// Creates a new settlement batch.
    /// </summary>
    public static ReimbursementSettlementBatch Create(
        string tenantId,
        FinancialOperationalPeriodId periodId,
        SettlementType type,
        IEnumerable<ExpenseClaimId> claimIds)
    {
        var ids = claimIds.ToList();
        if (ids.Count == 0)
        {
            throw new ArgumentException("A batch must contain at least one claim.");
        }

        return new ReimbursementSettlementBatch(
            SettlementBatchId.New(),
            tenantId,
            periodId,
            type,
            ids);
    }

    /// <summary>
    /// Gets the human-readable explanation for the current state.
    /// </summary>
    public string? LastExplanation { get; private set; }

    /// <summary>
    /// Transitions the batch to the executing state.
    /// </summary>
    public void StartExecution(string explanation)
    {
        if (Status != SettlementBatchStatus.Draft && Status != SettlementBatchStatus.PendingExecution)
        {
            throw new InvalidOperationException($"Cannot execute batch in state {Status}.");
        }

        Status = SettlementBatchStatus.Executing;
        ExecutedAt = DateTimeOffset.UtcNow;
        LastExplanation = explanation;
    }

    /// <summary>
    /// Records completion of the batch with an external reference.
    /// </summary>
    public void Complete(string externalReference, string explanation)
    {
        if (Status != SettlementBatchStatus.Executing)
        {
            throw new InvalidOperationException("Only executing batches can be completed.");
        }

        ExternalReference = externalReference;
        Status = SettlementBatchStatus.Completed;
        LastExplanation = explanation;
    }

    /// <summary>
    /// Finalizes the batch, locking it against any further modifications.
    /// </summary>
    public void FinalizeBatch(string explanation)
    {
        if (Status != SettlementBatchStatus.Completed && Status != SettlementBatchStatus.Reconciled)
        {
            throw new InvalidOperationException("Batch must be completed or reconciled before finalization.");
        }

        Status = SettlementBatchStatus.Finalized;
        FinalizedAt = DateTimeOffset.UtcNow;
        LastExplanation = explanation;
    }
}
