using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Aggregate representing one notification instance for one recipient.
/// Owns delivery attempts. Idempotent: creating a second message with the same
/// IdempotencyKey is a no-op â€” the existing message is returned unchanged.
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RecipientEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RecipientEmail { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationCategory Category { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationChannel PreferredChannel { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid TemplateId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RenderedSubject { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RenderedBodyHtml { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RenderedBodyText { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>Delivery held until this time (quiet hours, rate-limit back-off).</summary>
    public DateTimeOffset? ScheduleDeliverAfter { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? DeliveredOnUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadOnUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<NotificationDeliveryAttempt> Attempts => _attempts.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Defer(DateTimeOffset deliverAfter)
    {
        Status = NotificationStatus.Deferred;
        ScheduleDeliverAfter = deliverAfter;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Cancel()
    {
        if (Status == NotificationStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a delivered notification.");
        Status = NotificationStatus.Cancelled;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadOnUtc = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void QueueForDigest()
    {
        Status = NotificationStatus.DigestQueued;
    }
}
