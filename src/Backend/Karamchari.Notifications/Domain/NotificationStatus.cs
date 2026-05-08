namespace Karamchari.Notifications.Domain;

/// <summary>Lifecycle status of a notification message.</summary>
public enum NotificationStatus
{
    /// <summary>Created, not yet dispatched.</summary>
    Pending = 1,

    /// <summary>Deferred due to quiet hours or rate-limit — will be retried.</summary>
    Deferred = 2,

    /// <summary>Queued for digest batching.</summary>
    DigestQueued = 3,

    /// <summary>At least one channel delivered successfully.</summary>
    Delivered = 4,

    /// <summary>All delivery attempts failed after max retries.</summary>
    Failed = 5,

    /// <summary>Cancelled before dispatch (e.g., the triggering event was superseded).</summary>
    Cancelled = 6,
}
