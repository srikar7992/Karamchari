namespace Karamchari.Notifications.Domain;

/// <summary>Result of a single delivery attempt for one channel.</summary>
public enum DeliveryResult
{
    Success = 1,

    /// <summary>Transient failure — retryable.</summary>
    TransientFailure = 2,

    /// <summary>Permanent failure — do not retry (e.g., invalid address, unsubscribed).</summary>
    PermanentFailure = 3,

    /// <summary>Delivery skipped due to user opt-out for this category/channel.</summary>
    OptedOut = 4,

    /// <summary>Delivery deferred due to quiet hours.</summary>
    DeferredQuietHours = 5,

    /// <summary>Delivery deferred due to rate limit.</summary>
    DeferredRateLimit = 6,

    /// <summary>Delivery skipped — required data missing (e.g., no recipient email address).</summary>
    Skipped = 7,
}
