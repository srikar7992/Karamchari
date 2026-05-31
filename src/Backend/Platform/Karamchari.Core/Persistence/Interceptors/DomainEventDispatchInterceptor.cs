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
/// In production the dispatcher is the MassTransit-backed implementation Ã¢â‚¬â€
/// publishing inside <c>SavingChangesAsync</c> means MassTransit's bus outbox
/// captures the publishes into the OutboxMessage table atomically with the
/// aggregate's state change.
///
/// Sync <c>SavingChanges</c> is intentionally rejected when there are pending
/// events: domain event dispatch is async, and turning that into blocking
/// .GetAwaiter().GetResult() inside a SaveChanges call invites deadlocks.
/// EF 8+ encourages async-only paths anyway.
/// </summary>
public sealed partial class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchInterceptor"/> class.
    /// </summary>
    /// <param name="dispatcher">The domain event dispatcher.</param>
    /// <param name="logger">The logger.</param>
    public DomainEventDispatchInterceptor(
        IDomainEventDispatcher dispatcher,
        ILogger<DomainEventDispatchInterceptor> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Checks for pending domain events before saving changes synchronously.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is { } ctx && HasPendingDomainEvents(ctx))
        {
            throw new NotSupportedException(
                "Aggregates have pending domain events but SaveChanges was called synchronously. " +
                "Use SaveChangesAsync Ã¢â‚¬â€ domain event dispatch is async-only.");
        }

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Drains and dispatches domain events before saving changes asynchronously.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
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

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Dispatched {EventCount} domain event(s) to the bus outbox.", events.Count);
        }
    }

    private static bool HasPendingDomainEvents(DbContext context) =>
        context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Any(e => e.Entity.DomainEvents.Count > 0);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Dispatched {EventCount} domain event(s) to the bus outbox.")]
    static partial void LogDomainEventsDispatched(ILogger logger, int eventCount);
}
