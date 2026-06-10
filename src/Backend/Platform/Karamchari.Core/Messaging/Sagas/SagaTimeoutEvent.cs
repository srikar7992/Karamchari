// -----------------------------------------------------------------------
// <copyright file="SagaTimeoutEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Messaging.Sagas;

/// <summary>
/// Standard timeout event for workflows.
/// Sent by MassTransit scheduling when a state does not transition within the SLA.
/// </summary>
public sealed record SagaTimeoutEvent
{
    /// <summary>Correlation identifier for the saga.</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>Name of the state that timed out.</summary>
    public string StateName { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the timeout occurred.</summary>
    public DateTimeOffset TimeoutAtUtc { get; init; }
}
