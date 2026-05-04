namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Marker for an in-process domain event raised by an <see cref="AggregateRoot{TId}"/>.
/// Domain events are harvested by the Outbox interceptor on <c>SaveChangesAsync</c>
/// and persisted to the tenant outbox table within the same transaction â€”
/// MassTransit publishes them downstream.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Stable identifier for the event instance â€” used as the idempotency key downstream.</summary>
    Guid EventId { get; }

    /// <summary>UTC timestamp at which the event was raised on the aggregate.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
