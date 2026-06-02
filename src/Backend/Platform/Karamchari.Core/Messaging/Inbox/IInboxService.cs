namespace Karamchari.Core.Messaging.Inbox;

/// <summary>
/// Consumer-side idempotency service for the inbox deduplication pattern.
///
/// Implementations persist a record of each processed message by its broker-assigned
/// MessageId. The <see cref="InboxConsumerFilter{T}"/> uses this service to decide
/// whether to forward or skip an incoming message.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Returns <c>true</c> if the message identified by <paramref name="messageId"/>
    /// has already been processed by this consumer. The check is performed against
    /// persistent storage so it survives consumer restarts and replay scenarios.
    /// </summary>
    /// <param name="messageId">The broker-assigned message identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>
    /// Marks <paramref name="messageId"/> as successfully processed in persistent storage.
    /// Must be called only after the downstream message handler has completed without error.
    ///
    /// Implementations must be idempotent: calling this method on an already-processed
    /// ID must not throw — it must be a no-op.
    /// </summary>
    /// <param name="messageId">The broker-assigned message identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkProcessedAsync(Guid messageId, CancellationToken ct = default);
}
