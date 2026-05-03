namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Non-generic surface that lets infrastructure code (notably the Outbox
/// interceptor) discover and drain domain events from any tracked entity
/// without having to reason about <see cref="AggregateRoot{TId}"/>'s
/// generic parameter.
///
/// All <see cref="AggregateRoot{TId}"/> instances implement this interface.
/// Application code should not implement it directly.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
