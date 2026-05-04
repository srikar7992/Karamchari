using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Core.Messaging;

/// <summary>
/// Dispatches the domain events drained from aggregate roots during a save.
/// Implementations are expected to publish onto the messaging backbone in a
/// way that participates in the surrounding database transaction â€”
/// e.g. MassTransit's bus outbox captures publishes inside a SaveChanges
/// scope and persists them atomically with the aggregate's state change.
///
/// Core ships only the abstraction and a fail-closed null implementation.
/// The MassTransit-backed implementation lives outside Core so this assembly
/// stays free of messaging runtime dependencies.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Publishes the given events. Called by <c>DomainEventDispatchInterceptor</c>
    /// from inside <c>SavingChangesAsync</c>; implementations must complete
    /// before the transaction commits.
    /// </summary>
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);
}
