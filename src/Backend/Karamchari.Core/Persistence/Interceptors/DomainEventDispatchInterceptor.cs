using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Drains domain events from tracked aggregates on save and hands them to the
/// configured <see cref="IDomainEventDispatcher"/>.
///
/// In production the dispatcher is the MassTransit-backed implementation —
/// publishing inside <c>SavingChangesAsync</c> means MassTransit's bus outbox
/// captures the publishes into the OutboxMessage table atomically with the
/// aggregate's state change.
///
/// Sync <c>SavingChanges</c> is intentionally rejected when there are pending
/// events: domain event dispatch is async, and turning that into blocking
/// .GetAwaiter().GetResult() inside a SaveChanges call invites deadlocks.
/// EF 8+ encourages async-only paths anyway.
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    public DomainEventDispatchInterceptor(
        IDomainEventDispatcher dispatcher,
        ILogger<DomainEventDispatchInterceptor> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } ctx && HasPendingDomainEvents(ctx))
        {
            throw new NotSupportedException(
                "Aggregates have pending domain events but SaveChanges was called synchronously. " +
                "Use SaveChangesAsync — domain event dispatch is async-only.");
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } ctx)
        {
            await DrainAndDispatchAsync(ctx, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task DrainAndDispatchAsync(DbContext context, CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Drain BEFORE awaiting publish so a retry of SaveChanges (e.g. after
        // a concurrency conflict) doesn't double-emit.
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        await _dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Dispatched {EventCount} domain event(s) to the bus outbox.", events.Count);
    }

    private static bool HasPendingDomainEvents(DbContext context) =>
        context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Any(e => e.Entity.DomainEvents.Count > 0);
}
