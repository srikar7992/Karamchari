// -----------------------------------------------------------------------
// <copyright file="EmailChannelAdapter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Email;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Channels;

/// <summary>
/// Email delivery adapter. Delegates sending to IEmailProvider so the provider
/// (NullEmailProvider â†’ SendGrid â†’ SES) can be swapped without touching this class.
/// </summary>
public sealed class EmailChannelAdapter : INotificationChannelAdapter
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<EmailChannelAdapter> _logger;

    public EmailChannelAdapter(IEmailProvider emailProvider, ILogger<EmailChannelAdapter> logger)
    {
        _emailProvider = emailProvider;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationChannel Channel => NotificationChannel.Email;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task DeliverAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
                _logger.LogWarning(
                    "Email delivery skipped for message {MessageId}: RecipientEmail is empty",
                    message.Id);

            message.RecordDeliveryAttempt(
                NotificationChannel.Email,
                DeliveryResult.Skipped,
                providerMessageId: null,
                failureReason: "RecipientEmail not available");
            return;
        }

        var emailMessage = new EmailMessage(
            ToAddress: message.RecipientEmail,
            ToDisplayName: string.Empty,
            Subject: message.RenderedSubject,
            BodyHtml: message.RenderedBodyHtml,
            BodyText: message.RenderedBodyText,
            IdempotencyKey: message.IdempotencyKey);

        var result = await _emailProvider.SendAsync(emailMessage, cancellationToken);

        message.RecordDeliveryAttempt(
            NotificationChannel.Email,
            result.Success ? DeliveryResult.Success : DeliveryResult.PermanentFailure,
            providerMessageId: result.ProviderMessageId,
            failureReason: result.FailureReason);
    }
}
