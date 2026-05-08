using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Aggregate representing one notification instance for one recipient.
/// Owns delivery attempts. Idempotent: creating a second message with the same
/// IdempotencyKey is a no-op — the existing message is returned unchanged.
///
/// Idempotency key format: TenantId:TriggerEventId:Category:RecipientEmployeeId
/// (prevents duplicate notifications from event replay).
/// </summary>
public sealed class NotificationMessage : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<NotificationDeliveryAttempt> _attempts = [];
    private NotificationMessage() { /* EF materialization */ }

    private NotificationMessage(
        Guid id,
        string tenantId,
        Guid recipientEmployeeId,
        string recipientEmail,
        NotificationCategory category,
        NotificationChannel preferredChannel,
        Guid templateId,
        string renderedSubject,
        string renderedBodyHtml,
        string renderedBodyText,
        string idempotencyKey,
        DateTimeOffset? scheduleDeliverAfter) : base(id)
    {
        TenantId = tenantId;
        RecipientEmployeeId = recipientEmployeeId;
        RecipientEmail = recipientEmail;
        Category = category;
        PreferredChannel = preferredChannel;
        TemplateId = templateId;
        RenderedSubject = renderedSubject;
        RenderedBodyHtml = renderedBodyHtml;
        RenderedBodyText = renderedBodyText;
        IdempotencyKey = idempotencyKey;
        Status = NotificationStatus.Pending;
        ScheduleDeliverAfter = scheduleDeliverAfter;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid RecipientEmployeeId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public NotificationChannel PreferredChannel { get; private set; }
    public Guid TemplateId { get; private set; }
    public string RenderedSubject { get; private set; } = string.Empty;
    public string RenderedBodyHtml { get; private set; } = string.Empty;
    public string RenderedBodyText { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }

    /// <summary>Delivery held until this time (quiet hours, rate-limit back-off).</summary>
    public DateTimeOffset? ScheduleDeliverAfter { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? DeliveredOnUtc { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadOnUtc { get; private set; }

    public IReadOnlyCollection<NotificationDeliveryAttempt> Attempts => _attempts.AsReadOnly();

    public static NotificationMessage Create(
        string tenantId,
        Guid recipientEmployeeId,
        string recipientEmail,
        NotificationCategory category,
        NotificationChannel preferredChannel,
        Guid templateId,
        string renderedSubject,
        string renderedBodyHtml,
        string renderedBodyText,
        string idempotencyKey,
        DateTimeOffset? scheduleDeliverAfter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderedSubject);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderedBodyHtml);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderedBodyText);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new NotificationMessage(
            Guid.NewGuid(), tenantId, recipientEmployeeId, recipientEmail,
            category, preferredChannel, templateId,
            renderedSubject, renderedBodyHtml, renderedBodyText,
            idempotencyKey, scheduleDeliverAfter);
    }

    public void RecordDeliveryAttempt(
        NotificationChannel channel,
        DeliveryResult result,
        string? providerMessageId,
        string? failureReason)
    {
        var attempt = new NotificationDeliveryAttempt(
            Guid.NewGuid(), Id, channel, result, providerMessageId, failureReason,
            _attempts.Count + 1);
        _attempts.Add(attempt);

        Status = result switch
        {
            DeliveryResult.Success => NotificationStatus.Delivered,
            DeliveryResult.PermanentFailure or DeliveryResult.OptedOut => NotificationStatus.Failed,
            DeliveryResult.DeferredQuietHours or DeliveryResult.DeferredRateLimit => NotificationStatus.Deferred,
            _ => NotificationStatus.Pending,
        };

        if (result == DeliveryResult.Success)
            DeliveredOnUtc = DateTimeOffset.UtcNow;
    }

    public void Defer(DateTimeOffset deliverAfter)
    {
        Status = NotificationStatus.Deferred;
        ScheduleDeliverAfter = deliverAfter;
    }

    public void Cancel()
    {
        if (Status == NotificationStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered notification.");
        Status = NotificationStatus.Cancelled;
    }

    public void MarkRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadOnUtc = DateTimeOffset.UtcNow;
        }
    }

    public void QueueForDigest()
    {
        Status = NotificationStatus.DigestQueued;
    }
}
