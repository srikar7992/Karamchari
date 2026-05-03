using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Messaging;
using MassTransit;

namespace Karamchari.HR.Messaging;

/// <summary>
/// Publishes drained domain events through <see cref="IPublishEndpoint"/> so
/// MassTransit's bus outbox captures them inside the surrounding SaveChanges
/// transaction.
///
/// We Publish each event under its concrete runtime type — MassTransit serialises
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

    public MassTransitDomainEventDispatcher(IPublishEndpoint publishEndpoint)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        foreach (var @event in events)
        {
            // Publish under the concrete runtime type, not IDomainEvent — otherwise
            // every consumer would receive every event regardless of contract.
            await _publishEndpoint.Publish(@event, @event.GetType(), cancellationToken).ConfigureAwait(false);
        }
    }
}
