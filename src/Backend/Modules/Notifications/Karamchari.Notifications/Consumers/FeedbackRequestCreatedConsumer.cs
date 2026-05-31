using System.Globalization;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class FeedbackRequestCreatedConsumer : IConsumer<FeedbackRequestCreatedIntegrationEvent>
{
    private readonly INotificationOrchestrator _orchestrator;

    public FeedbackRequestCreatedConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<FeedbackRequestCreatedIntegrationEvent> context)
    {
        var e = context.Message;

        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.FeedbackRequested,
            Category: NotificationCategory.FeedbackRequests,
            RecipientEmployeeId: e.RequestedFromEmployeeId,
            RecipientEmail: e.RequestedFromEmail,
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["RecipientName"] = e.RequestedFromDisplayName,
                ["RequesterName"] = e.RequestedByDisplayName,
                ["FeedbackContext"] = e.FeedbackContext,
                ["DueDate"] = e.DueDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
