using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Operations;

public enum BulkOperationType
{
    BalanceAdjustment = 1,
    CarryForwardCorrection = 2,
    PolicyMigration = 3,
    AcquisitionOnboarding = 4,
    EncashmentBatch = 5,
    AccrualReset = 6,
}

public enum BulkOperationStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5,
    Cancelled = 6,
}

public enum BulkOperationItemStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4,
}

/// <summary>
/// Reusable bulk operation framework for mass balance adjustments, policy migrations,
/// carry-forward corrections, and acquisition onboarding. Future modules (Payroll, Expense)
/// extend BulkOperationType rather than building ad-hoc batch tables.
/// </summary>
public sealed class BulkOperation : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<BulkOperationItem> _items = [];

    private BulkOperation() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public BulkOperationType OperationType { get; private set; }
    public BulkOperationStatus Status { get; private set; }
    public string InitiatedBy { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TotalItems { get; private set; }
    public int SucceededItems { get; private set; }
    public int FailedItems { get; private set; }
    public int SkippedItems { get; private set; }
    public IReadOnlyCollection<BulkOperationItem> Items => _items.AsReadOnly();

    public static BulkOperation Create(
        string tenantId,
        BulkOperationType operationType,
        string initiatedBy,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(initiatedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new BulkOperation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OperationType = operationType,
            InitiatedBy = initiatedBy,
            Description = description,
            Status = BulkOperationStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public BulkOperationItem AddItem(Guid targetEntityId, string payload)
    {
        if (Status != BulkOperationStatus.Draft)
            throw new InvalidOperationException("Cannot add items after operation has started.");

        var item = BulkOperationItem.Create(Id, TenantId, targetEntityId, payload);
        _items.Add(item);
        TotalItems++;
        return item;
    }

    public void Start()
    {
        if (Status != BulkOperationStatus.Draft)
            throw new InvalidOperationException("Operation already started.");
        Status = BulkOperationStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void RecordItemResult(Guid itemId, bool success, string? failureReason = null)
    {
        BulkOperationItem item = _items.Find(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found.");

        item.Complete(success, failureReason);

        if (success) SucceededItems++;
        else FailedItems++;
    }

    public void SkipItem(Guid itemId, string reason)
    {
        BulkOperationItem item = _items.Find(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Item {itemId} not found.");
        item.Skip(reason);
        SkippedItems++;
    }

    public void Finish()
    {
        if (Status != BulkOperationStatus.InProgress)
            throw new InvalidOperationException("Operation not in progress.");

        Status = FailedItems == 0
            ? BulkOperationStatus.Completed
            : SucceededItems > 0
                ? BulkOperationStatus.PartiallyCompleted
                : BulkOperationStatus.Failed;

        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is BulkOperationStatus.Completed or BulkOperationStatus.Failed)
            throw new InvalidOperationException("Cannot cancel a finished operation.");
        Status = BulkOperationStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class BulkOperationItem
{
    private BulkOperationItem() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid BulkOperationId { get; private set; }
    public Guid TargetEntityId { get; private set; }
    /// <summary>JSON payload specific to the operation type (adjustment delta, new policyId, etc.).</summary>
    public string Payload { get; private set; } = string.Empty;
    public BulkOperationItemStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    internal static BulkOperationItem Create(Guid operationId, string tenantId, Guid targetEntityId, string payload)
    {
        return new BulkOperationItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BulkOperationId = operationId,
            TargetEntityId = targetEntityId,
            Payload = payload,
            Status = BulkOperationItemStatus.Pending,
        };
    }

    internal void Complete(bool success, string? failureReason)
    {
        Status = success ? BulkOperationItemStatus.Succeeded : BulkOperationItemStatus.Failed;
        FailureReason = failureReason;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    internal void Skip(string reason)
    {
        Status = BulkOperationItemStatus.Skipped;
        FailureReason = reason;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
