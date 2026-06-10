// -----------------------------------------------------------------------
// <copyright file="NullDomainEventDispatcher.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Core.Messaging;

/// <summary>
/// Default <see cref="IDomainEventDispatcher"/> registered by
/// <c>AddKaramchariCore</c>. Throws if any code path tries to dispatch a real
/// event â€” this is intentional: shipping a no-op publisher to production would
/// silently drop domain events.
///
/// A bounded context (or the API host) MUST register a real implementation
/// over this default before saving any aggregate that emits events.
/// </summary>
public sealed class NullDomainEventDispatcher : IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches domain events or fails when a real dispatcher has not been registered.
    /// </summary>
    public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException(
            $"No IDomainEventDispatcher is registered, but {events.Count} domain event(s) need publishing. " +
            "Register MassTransitDomainEventDispatcher (or another implementation) in your bounded context's AddKaramchari{Context} extension.");
    }
}
