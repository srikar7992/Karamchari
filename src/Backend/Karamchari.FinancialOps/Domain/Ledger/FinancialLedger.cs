using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Ledger;
using Karamchari.FinancialOps.Persistence;
using Microsoft.Extensions.Logging;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Domain service for interacting with the append-only financial ledger.
/// Enforces immutability, correlation, and event sourcing rules.
/// </summary>
public sealed class FinancialLedger : IFinancialLedger
{
    private readonly FinancialOpsDbContext _db;
    private readonly ILogger<FinancialLedger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialLedger"/> class.
    /// </summary>
    /// <param name="db">The financial ops DB context.</param>
    /// <param name="logger">The logger.</param>
    public FinancialLedger(FinancialOpsDbContext db, ILogger<FinancialLedger> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task RecordEntryAsync(
        string tenantId,
        EmployeeId employeeId,
        FinancialOperationalPeriodId periodId,
        FinancialOperationType type,
        decimal amount,
        CurrencyCode currency,
        FinancialCorrelationChain correlationChain,
        string explanation,
        string triggeredBy,
        FinancialReplayMetadata? replayMetadata = null,
        string? idempotencyKey = null,
        string? executionSequenceRef = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(employeeId);
        ArgumentNullException.ThrowIfNull(periodId);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(correlationChain);
        ArgumentException.ThrowIfNullOrWhiteSpace(explanation);
        ArgumentException.ThrowIfNullOrWhiteSpace(triggeredBy);

        var entry = WorkforceFinancialEntry.Record(
            tenantId,
            employeeId,
            periodId,
            type,
            amount,
            currency,
            correlationChain,
            replayMetadata ?? FinancialReplayMetadata.Empty,
            idempotencyKey,
            executionSequenceRef);

        _db.LedgerEntries.Add(entry);

        // Rule 8: Record transition/creation event
        var @event = FinancialOperationEvent.RecordTransition(
            tenantId,
            entry.Id.Value,
            nameof(WorkforceFinancialEntry),
            "Recorded",
            "None",
            FinancialOperationStatus.Executed.ToString(),
            explanation,
            triggeredBy,
            correlationChain);

        _db.OperationEvents.Add(@event);

        _logger.LogInformation(
            "Financial Entry Recorded: {EntryId} ({Type}) for Employee {EmployeeId}. Correlation: {CorrelationRoot}",
            entry.Id, type, employeeId, correlationChain.RootId);

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Contract for the financial ledger service.
/// </summary>
public interface IFinancialLedger
{
    /// <summary>
    /// Records a new immutable entry in the ledger along with its provenance event.
    /// </summary>
    Task RecordEntryAsync(
        string tenantId,
        EmployeeId employeeId,
        FinancialOperationalPeriodId periodId,
        FinancialOperationType type,
        decimal amount,
        CurrencyCode currency,
        FinancialCorrelationChain correlationChain,
        string explanation,
        string triggeredBy,
        FinancialReplayMetadata? replayMetadata = null,
        string? idempotencyKey = null,
        string? executionSequenceRef = null,
        CancellationToken ct = default);
}
