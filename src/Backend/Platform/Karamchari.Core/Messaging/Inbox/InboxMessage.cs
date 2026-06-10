// -----------------------------------------------------------------------
// <copyright file="InboxMessage.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Messaging.Inbox;

/// <summary>
/// Persisted record of a message received by a MassTransit consumer.
/// Used for idempotency deduplication: if a message with the same <see cref="Id"/>
/// has already been processed, the consumer skips it rather than executing again.
///
/// Stored in <c>dbo.InboxMessages</c> — shared infrastructure, not tenant-owned.
/// The primary key is the broker-assigned MessageId so a unique index provides
/// a database-level duplicate guard under concurrent delivery.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Broker-assigned message identifier. Maps to <c>ConsumeContext.MessageId</c>.
    /// This is the primary key; a unique index enforces exactly-once storage.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant that owns the message. Populated from the message envelope or
    /// MassTransit header set by the publishing tenant context.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Fully-qualified CLR type name of the consumed message (e.g.
    /// <c>Karamchari.HR.Contracts.EmployeeCreated</c>).
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized message payload for audit / replay purposes.
    /// Max size is enforced at the EF configuration layer.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the message was first received by this consumer.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the message handler completed successfully.
    /// <c>null</c> if the handler has not yet completed (e.g. in-flight or failed).
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// <c>true</c> once the handler has completed successfully and
    /// <see cref="ProcessedAt"/> has been set. Used as the fast-path check
    /// in the idempotency filter before consulting the database.
    /// </summary>
    public bool IsProcessed { get; set; }
}
