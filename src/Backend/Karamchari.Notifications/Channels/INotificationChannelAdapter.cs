using Karamchari.Notifications.Domain;

namespace Karamchari.Notifications.Channels;

/// <summary>
/// Abstraction over a single delivery channel (in-app, email, push).
/// The orchestrator calls every registered adapter whose channel matches the
/// recipient's enabled preferences. Adapters record their own delivery attempts
/// on the message aggregate â€” they never directly update <see cref="NotificationStatus"/>.
/// </summary>
public interface INotificationChannelAdapter
{
    NotificationChannel Channel { get; }

    Task DeliverAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
