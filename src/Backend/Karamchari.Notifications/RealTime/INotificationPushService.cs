namespace Karamchari.Notifications.RealTime;

/// <summary>
/// Pushes real-time notification signals to connected clients via SignalR.
/// Called by the orchestrator after the notification is persisted.
/// Fire-and-forget â€” failure here must never roll back the delivery.
/// </summary>
public interface INotificationPushService
{
    /// <summary>
    /// Pushes an unread-count update and the new notification DTO to the
    /// recipient's SignalR group ({tenantId}:{employeeId}).
    /// </summary>
    Task PushAsync(
        string tenantId,
        Guid recipientEmployeeId,
        int unreadCount,
        NotificationPushDto notification,
        CancellationToken cancellationToken = default);
}

/// <summary>Minimal DTO sent over the SignalR wire.</summary>
public sealed record NotificationPushDto(
    Guid Id,
    string Category,
    string Subject,
    string BodyText,
    bool IsRead,
    DateTimeOffset CreatedAt);
