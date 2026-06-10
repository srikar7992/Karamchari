// -----------------------------------------------------------------------
// <copyright file="AggregateRoot.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Base class for aggregate roots. An aggregate is the consistency boundary Ã¢â‚¬â€
/// state changes that span multiple entities must go through the root.
///
/// Aggregate roots accumulate <see cref="IDomainEvent"/>s during a unit of work.
/// The Outbox interceptor (see <c>DomainEventOutboxInterceptor</c>) drains
/// these events on <c>SaveChangesAsync</c> and persists them to the tenant
/// outbox table inside the same transaction.
/// </summary>
/// <typeparam name="TId">The aggregate identifier type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class for EF Core.
    /// </summary>
    protected AggregateRoot() { /* EF materialization */ }

    /// <summary>Read-only view of pending domain events. The collection is mutated only via <see cref="RaiseDomainEvent"/> and <see cref="ClearDomainEvents"/>.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records a domain event. Events are dispatched only after the aggregate's
    /// state has been committed Ã¢â‚¬â€ never publish synchronously inside a method.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _domainEvents.Add(@event);
    }

    /// <summary>
    /// Drains the pending event list. Called by the Outbox interceptor after
    /// the events have been persisted to the outbox table.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
