using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.EventSourcing;

/// <summary>
/// Persistence implementation of the bi-temporal event store, generic over the DbContext to enforce modular schema isolation.
/// </summary>
/// <typeparam name="TContext">The DB context type that inherits from KaramchariDbContext.</typeparam>
public sealed class BiTemporalEventStore<TContext> : IEventStore
    where TContext : KaramchariDbContext
{
    private readonly TContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiTemporalEventStore{TContext}"/> class.
    /// </summary>
    /// <param name="dbContext">The db context.</param>
    public BiTemporalEventStore(TContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AppendEventAsync(
        string tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventType,
        object eventData,
        DateTimeOffset effectiveUtc,
        Guid correlationId,
        Guid causationId,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(eventData);

        var eventJson = JsonSerializer.Serialize(eventData);

        // Calculate the next version sequence number for this aggregate
        var lastVersion = await _dbContext.StoredEvents
            .Where(e => e.TenantId == tenantId && e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .Select(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var storedEvent = new StoredEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AggregateId = aggregateId,
            AggregateType = aggregateType ?? string.Empty,
            EventType = eventType,
            EventDataJson = eventJson,
            Version = lastVersion + 1,
            CorrelationId = correlationId,
            CausationId = causationId,
            MetadataJson = metadataJson ?? string.Empty,
            EffectiveUtc = effectiveUtc,
            TransactionUtc = DateTimeOffset.UtcNow
        };

        _dbContext.StoredEvents.Add(storedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<StoredEvent>> GetEventsAsOfAsync(
        string tenantId,
        Guid aggregateId,
        DateTimeOffset effectiveAsOf,
        DateTimeOffset transactionAsOf,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await _dbContext.StoredEvents
            .Where(e => e.TenantId == tenantId &&
                        e.AggregateId == aggregateId &&
                        e.EffectiveUtc <= effectiveAsOf &&
                        e.TransactionUtc <= transactionAsOf)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);
    }
}
