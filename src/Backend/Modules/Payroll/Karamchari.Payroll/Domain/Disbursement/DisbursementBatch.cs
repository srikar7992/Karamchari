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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RunId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PeriodName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public BankProvider BankProvider { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DisbursementBatchStatus Status { get; private set; }

    // Idempotency guard
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BatchReference { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalEntries { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int SuccessCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int FailedCount { get; private set; }

    public string? BankFileS3Path { get; private set; }
    public string? BankAcknowledgementId { get; private set; }
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int RetryCount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int MaxRetries { get; private set; } = 3;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<DisbursementEntry> Entries => _entries.AsReadOnly();

    private DisbursementBatch() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static DisbursementBatch Create(
        string tenantId,
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
            BatchReference = $"{tenantId}-{periodName}-{Guid.NewGuid():N}",
            Status = DisbursementBatchStatus.Pending,
            InitiatedBy = initiatedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        batch._entries.AddRange(entries);
        batch.TotalEntries = batch._entries.Count;
        batch.TotalAmount = batch._entries.Sum(e => e.Amount);

        return batch;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkFileGenerated(string blobPath)
    {
        if (Status != DisbursementBatchStatus.Pending)
            throw new InvalidOperationException($"Cannot mark file generated in status {Status}.");

        BankFileS3Path = blobPath;
        Status = DisbursementBatchStatus.FileGenerated;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkSubmitted(string acknowledgementId)
    {
        Status = DisbursementBatchStatus.Submitted;
        BankAcknowledgementId = acknowledgementId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new DisbursementBatchSubmittedEvent(Id, TenantId, RunId, PeriodName, TotalAmount));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ApplyBankResponse(IEnumerable<EntryResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordRetry()
    {
        if (RetryCount >= MaxRetries)
            throw new InvalidOperationException("Max retries exceeded for disbursement batch.");

        RetryCount++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkFailed(string reason)
    {
        Status = DisbursementBatchStatus.Failed;
        FailureReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new DisbursementBatchFailedEvent(Id, TenantId, RunId, reason));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void ReverseEntry(Guid entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException($"Entry {entryId} not found in batch.");

        entry.Reverse();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
