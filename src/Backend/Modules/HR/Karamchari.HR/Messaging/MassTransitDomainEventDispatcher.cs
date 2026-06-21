// -----------------------------------------------------------------------
// <copyright file="MassTransitDomainEventDispatcher.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
    private readonly IServiceProvider _serviceProvider;
    private readonly ITenantProvider _tenantProvider;

    public MassTransitDomainEventDispatcher(IServiceProvider serviceProvider, ITenantProvider tenantProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(tenantProvider);
        _serviceProvider = serviceProvider;
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

        var publishEndpoint = (IPublishEndpoint)_serviceProvider.GetService(typeof(IPublishEndpoint))!;
        var tenantId = _tenantProvider.TryGetCurrentTenantId(out var tId) ? tId : "system";

        string? correlationId = null;
        string? causationId = null;
        if (_tenantProvider.TryGetTenant(out var executionEnv) && executionEnv is not null)
        {
            correlationId = executionEnv.CorrelationId;
            causationId = executionEnv.RequestId.ToString();
        }

        foreach (var @event in events)
        {
            var eventType = @event.GetType();

            // Wrap is a generic STATIC method: EnterpriseEventEnvelope<TPayload>.Wrap<T>(...).
            // Closing the declaring type is not enough — the method's own type parameter T must
            // be bound with MakeGenericMethod, otherwise MethodInfo.Invoke throws
            // "Late bound operations cannot be performed ... ContainsGenericParameters is true".
            var wrapMethod = typeof(EnterpriseEventEnvelope<>)
                .MakeGenericType(eventType)
                .GetMethod("Wrap", BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(eventType);

            if (wrapMethod != null)
            {
                var envelope = wrapMethod.Invoke(null, new object?[]
                {
                    @event,
                    tenantId,
                    eventType.Name,
                    "1.0",
                    correlationId,
                    causationId,
                    "karamchari.hr"
                });

                if (envelope != null)
                {
                    await publishEndpoint.Publish(envelope, envelope.GetType(), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
