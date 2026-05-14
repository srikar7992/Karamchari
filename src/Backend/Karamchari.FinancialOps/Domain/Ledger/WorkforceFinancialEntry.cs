using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// The immutable, append-only operational financial entry in the workforce ledger.
/// Tracks every settlement, deduction, adjustment, and recovery.
/// </summary>
public sealed class WorkforceFinancialEntry : AggregateRoot<WorkforceFinancialEntryId>, ITenantOwned
{
    private WorkforceFinancialEntry(
        WorkforceFinancialEntryId id,
        string tenantId,
        EmployeeId employeeId,
        FinancialOperationalPeriodId periodId,
        FinancialOperationType type,
        decimal amount,
        CurrencyCode currency,
        FinancialCorrelationChain correlationChain,
        FinancialReplayMetadata replayMetadata,
        string? idempotencyKey = null,
        string? executionSequenceRef = null)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        PeriodId = periodId;
        Type = type;
        Amount = amount;
        Currency = currency;
        CorrelationChain = correlationChain;
        ReplayMetadata = replayMetadata;
        Status = FinancialOperationStatus.Executed;
        IdempotencyKey = idempotencyKey;
        ExecutionSequenceRef = executionSequenceRef;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private WorkforceFinancialEntry()
    {
        // EF Core
        TenantId = string.Empty;
        EmployeeId = null!;
        PeriodId = null!;
        Currency = null!;
        CorrelationChain = null!;
        ReplayMetadata = null!;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public EmployeeId EmployeeId { get; private set; }

    /// <summary>
    /// Gets the financial operational period identifier.
    /// </summary>
    public FinancialOperationalPeriodId PeriodId { get; private set; }

    /// <summary>
    /// Gets the type of the financial operation.
    /// </summary>
    public FinancialOperationType Type { get; private set; }

    /// <summary>
    /// Gets the operational status of the entry.
    /// </summary>
    public FinancialOperationStatus Status { get; private set; }

    /// <summary>
    /// Gets the amount recorded in this entry.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets the currency of the amount.
    /// </summary>
    public CurrencyCode Currency { get; private set; }

    /// <summary>
    /// Gets the correlation chain for this entry.
    /// </summary>
    public FinancialCorrelationChain CorrelationChain { get; private set; }

    /// <summary>
    /// Gets the replay metadata for this entry.
    /// </summary>
    public FinancialReplayMetadata ReplayMetadata { get; private set; }

    /// <summary>
    /// Gets the optional idempotency key to prevent duplicate entries.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// Gets the optional execution sequence reference.
    /// </summary>
    public string? ExecutionSequenceRef { get; private set; }

    /// <summary>
    /// Gets the timestamp when this entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Records a new financial entry in the ledger.
    /// </summary>
    public static WorkforceFinancialEntry Record(
        string tenantId,
        EmployeeId employeeId,
        FinancialOperationalPeriodId periodId,
        FinancialOperationType type,
        decimal amount,
        CurrencyCode currency,
        FinancialCorrelationChain correlationChain,
        FinancialReplayMetadata replayMetadata,
        string? idempotencyKey = null,
        string? executionSequenceRef = null)
    {
        return new WorkforceFinancialEntry(
            WorkforceFinancialEntryId.New(),
            tenantId,
            employeeId,
            periodId,
            type,
            amount,
            currency,
            correlationChain,
            replayMetadata,
            idempotencyKey,
            executionSequenceRef);
    }
}
