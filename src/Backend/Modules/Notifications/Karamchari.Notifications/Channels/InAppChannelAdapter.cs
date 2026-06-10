// -----------------------------------------------------------------------
// <copyright file="InAppChannelAdapter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Channels;

/// <summary>
/// In-app delivery: the notification is already persisted in NotificationMessages â€”
/// recording a successful delivery attempt is sufficient for the inbox to surface it.
/// </summary>
public sealed class InAppChannelAdapter : INotificationChannelAdapter
{
    private readonly ILogger<InAppChannelAdapter> _logger;

    public InAppChannelAdapter(ILogger<InAppChannelAdapter> logger) => _logger = logger;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationChannel Channel => NotificationChannel.InApp;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task DeliverAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        message.RecordDeliveryAttempt(
            channel: NotificationChannel.InApp,
            result: DeliveryResult.Success,
            providerMessageId: null,
            failureReason: null);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(
                "In-app delivery recorded for message {MessageId}, recipient {RecipientId}",
                message.Id, message.RecipientEmployeeId);

        return Task.CompletedTask;
    }
}
