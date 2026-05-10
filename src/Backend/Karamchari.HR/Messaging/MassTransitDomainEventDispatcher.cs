using System.Reflection;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Messaging;
using Karamchari.Core.Multitenancy;
using MassTransit;

namespace Karamchari.HR.Messaging;

/// <summary>
/// Publishes drained domain events through <see cref="IPublishEndpoint"/> so
/// MassTransit's bus outbox captures them inside the surrounding SaveChanges
/// transaction.
/// </summary>
public sealed class MassTransitDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITenantProvider _tenantProvider;

    public MassTransitDomainEventDispatcher(IPublishEndpoint publishEndpoint, ITenantProvider tenantProvider)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        _publishEndpoint = publishEndpoint;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            return;
        }

        var tenantId = _tenantProvider.TryGetCurrentTenantId(out var tId) ? tId : "system";

        foreach (var @event in events)
        {
            var eventType = @event.GetType();

            // We use reflection to invoke the generic Wrap method
            var wrapMethod = typeof(EnterpriseEventEnvelope<>)
                .MakeGenericType(eventType)
                .GetMethod("Wrap", BindingFlags.Public | BindingFlags.Static);

            if (wrapMethod != null)
            {
                var envelope = wrapMethod.Invoke(null, new object?[]
                {
                    @event,
                    tenantId,
                    eventType.Name,
                    "1.0",
                    null, // correlationId will be populated by MassTransit context
                    null, // causationId 
                    "karamchari.hr"
                });

                if (envelope != null)
                {
                    await _publishEndpoint.Publish(envelope, envelope.GetType(), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
