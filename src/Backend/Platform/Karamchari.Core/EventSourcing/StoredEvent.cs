// -----------------------------------------------------------------------
// <copyright file="StoredEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.EventSourcing;

/// <summary>
/// Represents a bi-temporal domain event stored in the event store.
/// </summary>
public sealed class StoredEvent : ITenantOwned
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for database isolation.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the aggregate identifier this event belongs to.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the aggregate type name (for routing and serialization).
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON serialized event payload.
    /// </summary>
    public string EventDataJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version sequence of the event within the aggregate stream.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for tracing.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation identifier for tracing.
    /// </summary>
    public Guid CausationId { get; set; }

    /// <summary>
    /// Gets or sets the JSON serialized event metadata.
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the effective date/time of the state change (business time).
    /// </summary>
    public DateTimeOffset EffectiveUtc { get; set; }

    /// <summary>
    /// Gets or sets the transaction date/time when the event was recorded (system time).
    /// </summary>
    public DateTimeOffset TransactionUtc { get; set; }

    /// <summary>
    /// Gets or sets the global sequence position of the event across the entire stream.
    /// </summary>
    public long Position { get; private set; }
}
