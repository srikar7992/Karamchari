using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Karamchari.Core.EventSourcing;

/// <summary>
/// Defines the contract for a Bi-Temporal Event Store that supports retroactive (effective-dated) changes.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a new event to the aggregate event stream.
    /// </summary>
    Task AppendEventAsync(
        string tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventType,
        object eventData,
        DateTimeOffset effectiveUtc,
        Guid correlationId,
        Guid causationId,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of events for an aggregate as they were known as of a specific transaction time
    /// and effective date.
    /// </summary>
    Task<List<StoredEvent>> GetEventsAsOfAsync(
        string tenantId,
        Guid aggregateId,
        DateTimeOffset effectiveAsOf,
        DateTimeOffset transactionAsOf,
        CancellationToken cancellationToken = default);
}
