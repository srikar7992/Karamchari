namespace Karamchari.Notifications.Domain;

/// <summary>Delivery channel for a notification.</summary>
public enum NotificationChannel
{
    /// <summary>Stored in-app; displayed in the notification center.</summary>
    InApp = 1,

    /// <summary>Delivered via email (Azure Communication Services).</summary>
    Email = 2,

    /// <summary>Mobile push notification (Azure Notification Hubs â€” Phase 2).</summary>
    Push = 3,

    /// <summary>Webhook delivery to Slack or Teams (tenant-configured â€” Phase 2).</summary>
    Webhook = 4,
}
