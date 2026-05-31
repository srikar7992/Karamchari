using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.RealTime;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class HubNotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<HubNotificationPushService> _logger;

    public HubNotificationPushService(
        IHubContext<NotificationHub> hub,
        ILogger<HubNotificationPushService> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task PushAsync(
        string tenantId,
        Guid recipientEmployeeId,
        int unreadCount,
        NotificationPushDto notification,
        CancellationToken cancellationToken = default)
    {
        var group = NotificationHub.GroupName(tenantId, recipientEmployeeId);

        try
        {
            await _hub.Clients.Group(group)
                .SendAsync("UnreadCountUpdated", unreadCount, cancellationToken);

            await _hub.Clients.Group(group)
                .SendAsync("NotificationReceived", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            // Real-time push failure must never fail the orchestration unit-of-work.
            _logger.LogWarning(ex,
                "SignalR push failed for group {Group}; notification {NotificationId} already persisted",
                group, notification.Id);
        }
    }
}
