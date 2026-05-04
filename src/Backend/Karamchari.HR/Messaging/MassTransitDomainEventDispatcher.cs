using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Messaging;
using MassTransit;

namespace Karamchari.HR.Messaging;

/// <summary>
/// Publishes drained domain events through <see cref="IPublishEndpoint"/> so
/// MassTransit's bus outbox captures them inside the surrounding SaveChanges
/// transaction.
///
/// We Publish each event under its concrete runtime type â€” MassTransit serialises
/// the message envelope with the actual type name so consumers can subscribe to
/// the specific contract (<c>EmployeeHired</c>) rather than the marker
/// <c>IDomainEvent</c>.
///
/// This implementation lives in <c>Karamchari.HR</c> for now because HR already
/// references MassTransit. When Payroll lands we'll lift it into a shared
/// <c>Karamchari.Messaging</c> project.
/// </summary>
public sealed class MassTransitDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    public MassTransitDomainEventDispatcher(IPublishEndpoint publishEndpoint)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Dispatches a collection of domain events asynchronously.
    /// </summary>
    /// <param name="events">The collection of domain events.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            return;
        }

        foreach (var @event in events)
        {
            // Publish under the concrete runtime type, not IDomainEvent â€” otherwise
            // every consumer would receive every event regardless of contract.
            await _publishEndpoint.Publish(@event, @event.GetType(), cancellationToken).ConfigureAwait(false);
        }
    }
}
