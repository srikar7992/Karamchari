using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Aggregate representing one generated digest for one recipient covering
/// one time window and one frequency type (Daily / Weekly).
///
/// A DigestBatch is created by DigestAggregatorService from the pool of
/// NotificationMessages whose Status == DigestQueued. After delivery, each
/// source message is transitioned to Delivered so it won't be re-included.
///
/// Idempotency key: TenantId:RecipientEmployeeId:DigestType:WindowKey
/// where WindowKey = "2026-05-09" for daily or "2026-W19" for weekly.
/// </summary>
public sealed class DigestBatch : AggregateRoot<Guid>, ITenantOwned
{
    private DigestBatch() { /* EF materialization */ }

    private DigestBatch(
        Guid id,
        string tenantId,
        Guid recipientEmployeeId,
        string recipientEmail,
        DigestType digestType,
        string windowKey,
        string idempotencyKey,
        IReadOnlyList<Guid> sourceMessageIds,
        string renderedSubject,
        string renderedBodyHtml,
        string renderedBodyText,
        DateTimeOffset? scheduleDeliverAfter) : base(id)
    {
        TenantId = tenantId;
        RecipientEmployeeId = recipientEmployeeId;
        RecipientEmail = recipientEmail;
        DigestType = digestType;
        WindowKey = windowKey;
        IdempotencyKey = idempotencyKey;
        SourceMessageIds = sourceMessageIds;
        RenderedSubject = renderedSubject;
        RenderedBodyHtml = renderedBodyHtml;
        RenderedBodyText = renderedBodyText;
        ScheduleDeliverAfter = scheduleDeliverAfter;
        Status = DigestBatchStatus.Pending;
        CreatedOnUtc = DateTimeOffset.UtcNow;
        MessageCount = sourceMessageIds.Count;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid RecipientEmployeeId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public DigestType DigestType { get; private set; }

    /// <summary>"2026-05-09" for daily; "2026-W19" for weekly.</summary>
    public string WindowKey { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>JSON-serialized list of source NotificationMessage IDs included in this digest.</summary>
    public IReadOnlyList<Guid> SourceMessageIds { get; private set; } = [];

    public string RenderedSubject { get; private set; } = string.Empty;
    public string RenderedBodyHtml { get; private set; } = string.Empty;
    public string RenderedBodyText { get; private set; } = string.Empty;
    public int MessageCount { get; private set; }

    public DigestBatchStatus Status { get; private set; }
    public DateTimeOffset? ScheduleDeliverAfter { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? DeliveredOnUtc { get; private set; }

    public static DigestBatch Create(
        string tenantId,
        Guid recipientEmployeeId,
        string recipientEmail,
        DigestType digestType,
        string windowKey,
        IReadOnlyList<Guid> sourceMessageIds,
        string renderedSubject,
        string renderedBodyHtml,
        string renderedBodyText,
        DateTimeOffset? scheduleDeliverAfter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(windowKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderedSubject);
        ArgumentNullException.ThrowIfNull(sourceMessageIds);

        if (sourceMessageIds.Count == 0)
            throw new ArgumentException("Digest batch must contain at least one source message.", nameof(sourceMessageIds));

        var idempotencyKey = $"{tenantId}:{recipientEmployeeId}:{digestType}:{windowKey}";

        return new DigestBatch(
            Guid.NewGuid(), tenantId, recipientEmployeeId, recipientEmail,
            digestType, windowKey, idempotencyKey, sourceMessageIds,
            renderedSubject, renderedBodyHtml, renderedBodyText, scheduleDeliverAfter);
    }

    public void MarkDelivered()
    {
        Status = DigestBatchStatus.Delivered;
        DeliveredOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = DigestBatchStatus.Failed;
    }

    public void Cancel() => Status = DigestBatchStatus.Cancelled;
}

public enum DigestType
{
    Daily = 1,
    Weekly = 2,
}

public enum DigestBatchStatus
{
    Pending = 1,
    Delivering = 2,
    Delivered = 3,
    Failed = 4,
    Cancelled = 5,
}
