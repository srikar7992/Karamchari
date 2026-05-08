namespace Karamchari.Core.Messaging.Outbox;

/// <summary>
/// Persisted record of a message the relay gave up on after exhausting retries.
/// Stored in dbo.OutboxDeadLetter — shared infrastructure, not tenant-owned.
/// </summary>
public sealed class OutboxDeadLetterMessage
{
    /// <summary>EF identity column; never set by application code.</summary>
    public long Id { get; private set; }

    /// <summary>Original MassTransit message identifier (preserved for idempotency audits).</summary>
    public Guid MessageId { get; init; }

    /// <summary>The dbo.OutboxState row this message belonged to.</summary>
    public Guid OutboxId { get; init; }

    /// <summary>Semicolon-delimited MassTransit URN type names.</summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>Full MassTransit JSON envelope as stored in OutboxMessage.Body.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Original MassTransit headers JSON, if any.</summary>
    public string? Headers { get; init; }

    /// <summary>Destination address recorded in the original OutboxMessage, if any.</summary>
    public string? DestinationAddress { get; init; }

    /// <summary>Last exception message that caused exhaustion.</summary>
    public string FailureReason { get; init; } = string.Empty;

    /// <summary>Number of publish attempts made before giving up.</summary>
    public int RetryCount { get; init; }

    /// <summary>UTC timestamp when the message was moved to the dead-letter store.</summary>
    public DateTime FailedAt { get; init; }
}
