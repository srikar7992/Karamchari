// -----------------------------------------------------------------------
// <copyright file="TransportConfigurationGuard.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Messaging;

/// <summary>
/// Fail-fast guard against running outside Development with no durable message
/// transport configured. Without this, a misconfigured non-development host
/// would silently select the in-memory transport: messages would never cross
/// process boundaries, the outbox relay would publish into a void, and async
/// cross-module flows would break with no operator-visible signal.
///
/// Certification finding C-4 (see CERTIFICATION.md).
/// </summary>
public static class TransportConfigurationGuard
{
    /// <summary>
    /// Throws when <paramref name="isDevelopment"/> is <see langword="false"/> and neither
    /// a RabbitMQ nor an Azure Service Bus connection string is present. The in-memory
    /// transport is permitted only in Development.
    /// </summary>
    /// <param name="isDevelopment">Whether the host environment is Development.</param>
    /// <param name="rabbitMqConnectionString">The configured RabbitMQ connection string, if any.</param>
    /// <param name="serviceBusConnectionString">The configured Azure Service Bus connection string, if any.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a non-Development host has no durable transport configured.
    /// </exception>
    public static void EnsureConfigured(
        bool isDevelopment,
        string? rabbitMqConnectionString,
        string? serviceBusConnectionString)
    {
        if (isDevelopment)
        {
            return;
        }

        var hasRabbitMq = !string.IsNullOrWhiteSpace(rabbitMqConnectionString);
        var hasServiceBus = !string.IsNullOrWhiteSpace(serviceBusConnectionString);

        if (!hasRabbitMq && !hasServiceBus)
        {
            throw new InvalidOperationException(
                "No production message transport configured. A non-Development host requires a "
                + "'RabbitMQ' or 'AzureServiceBus' connection string. The in-memory transport is "
                + "permitted only in Development because it loses messages across process restarts "
                + "and never crosses process boundaries (certification finding C-4).");
        }
    }
}
