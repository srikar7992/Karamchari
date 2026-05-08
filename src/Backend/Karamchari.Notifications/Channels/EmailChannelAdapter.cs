using Karamchari.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Channels;

/// <summary>
/// Email delivery adapter — Phase 1 stub.
/// Logs the send intent and marks delivery as successful for development / testing.
/// Phase 2: wire to Azure Communication Services email sender.
/// </summary>
public sealed class EmailChannelAdapter : INotificationChannelAdapter
{
    private readonly ILogger<EmailChannelAdapter> _logger;

    public EmailChannelAdapter(ILogger<EmailChannelAdapter> logger) => _logger = logger;

    public NotificationChannel Channel => NotificationChannel.Email;

    public Task DeliverAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        // Phase 1 — log only; no real send.
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "[EMAIL-STUB] Would send to {RecipientEmail} | Subject: {Subject} | MessageId: {MessageId}",
                message.RecipientEmail, message.RenderedSubject, message.Id);

        message.RecordDeliveryAttempt(
            channel: NotificationChannel.Email,
            result: DeliveryResult.Success,
            providerMessageId: $"stub-{message.Id}",
            failureReason: null);

        return Task.CompletedTask;
    }
}
