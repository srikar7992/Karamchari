using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Append-only record of one delivery attempt for one channel.
/// Never updated — new attempt = new row. Supports retry auditing and bounce tracking.
/// </summary>
public sealed class NotificationDeliveryAttempt : Entity<Guid>
{
    private NotificationDeliveryAttempt() { /* EF materialization */ }

    internal NotificationDeliveryAttempt(
        Guid id,
        Guid notificationMessageId,
        NotificationChannel channel,
        DeliveryResult result,
        string? providerMessageId,
        string? failureReason,
        int attemptNumber) : base(id)
    {
        NotificationMessageId = notificationMessageId;
        Channel = channel;
        Result = result;
        ProviderMessageId = providerMessageId;
        FailureReason = failureReason;
        AttemptNumber = attemptNumber;
        AttemptedOnUtc = DateTimeOffset.UtcNow;
    }

    public Guid NotificationMessageId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DeliveryResult Result { get; private set; }

    /// <summary>Message ID returned by the delivery provider (e.g., Azure Communication Services message ID).</summary>
    public string? ProviderMessageId { get; private set; }

    public string? FailureReason { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTimeOffset AttemptedOnUtc { get; private set; }
}
