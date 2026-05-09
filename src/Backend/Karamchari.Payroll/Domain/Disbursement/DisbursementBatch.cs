using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Disbursement.Events;

namespace Karamchari.Payroll.Domain.Disbursement;

/// <summary>
/// Aggregate for a bank disbursement batch covering one payroll run.
/// Idempotency guard: unique (RunId, TenantId) prevents duplicate batches.
/// Entry-level idempotency: unique (BatchId, EmployeeId) per DisbursementEntry.
/// </summary>
public sealed class DisbursementBatch : AggregateRoot<Guid>
{
    private readonly List<DisbursementEntry> _entries = [];

    public Guid TenantId { get; private set; }
    public Guid RunId { get; private set; }
    public string PeriodName { get; private set; } = string.Empty;
    public BankProvider BankProvider { get; private set; }
    public DisbursementBatchStatus Status { get; private set; }

    // Idempotency guard
    public string BatchReference { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }
    public int TotalEntries { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailedCount { get; private set; }

    public string? BankFileS3Path { get; private set; }
    public string? BankAcknowledgementId { get; private set; }
    public string? FailureReason { get; private set; }

    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;

    public string InitiatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<DisbursementEntry> Entries => _entries.AsReadOnly();

    private DisbursementBatch() { }

    public static DisbursementBatch Create(
        Guid tenantId,
        Guid runId,
        string periodName,
        BankProvider provider,
        IEnumerable<DisbursementEntry> entries,
        string initiatedBy)
    {
        var batch = new DisbursementBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RunId = runId,
            PeriodName = periodName,
            BankProvider = provider,
            BatchReference = $"{tenantId:N}-{periodName}-{Guid.NewGuid():N}",
            Status = DisbursementBatchStatus.Pending,
            InitiatedBy = initiatedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        batch._entries.AddRange(entries);
        batch.TotalEntries = batch._entries.Count;
        batch.TotalAmount = batch._entries.Sum(e => e.Amount);

        return batch;
    }

    public void MarkFileGenerated(string blobPath)
    {
        if (Status != DisbursementBatchStatus.Pending)
            throw new InvalidOperationException($"Cannot mark file generated in status {Status}.");

        BankFileS3Path = blobPath;
        Status = DisbursementBatchStatus.FileGenerated;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSubmitted(string acknowledgementId)
    {
        Status = DisbursementBatchStatus.Submitted;
        BankAcknowledgementId = acknowledgementId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new DisbursementBatchSubmittedEvent(Id, TenantId, RunId, PeriodName, TotalAmount));
    }

    public void ApplyBankResponse(IEnumerable<EntryResult> results)
    {
        foreach (var result in results)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == result.EntryId);
            if (entry is null) continue;

            if (result.IsSuccess)
                entry.MarkSuccess(result.BankTransactionId!);
            else
                entry.MarkFailed(result.FailureReason ?? "Unknown bank failure");
        }

        SuccessCount = _entries.Count(e => e.Status == DisbursementEntryStatus.Success);
        FailedCount = _entries.Count(e => e.Status == DisbursementEntryStatus.Failed);

        Status = FailedCount == 0
            ? DisbursementBatchStatus.Completed
            : DisbursementBatchStatus.PartiallyProcessed;

        if (Status == DisbursementBatchStatus.Completed)
            CompletedAtUtc = DateTimeOffset.UtcNow;

        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new DisbursementBatchCompletedEvent(Id, TenantId, RunId, SuccessCount, FailedCount));
    }

    public void RecordRetry()
    {
        if (RetryCount >= MaxRetries)
            throw new InvalidOperationException("Max retries exceeded for disbursement batch.");

        RetryCount++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = DisbursementBatchStatus.Failed;
        FailureReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new DisbursementBatchFailedEvent(Id, TenantId, RunId, reason));
    }

    public void ReverseEntry(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException($"Entry {entryId} not found in batch.");

        entry.Reverse();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
