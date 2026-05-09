using System.Globalization;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ReviewAssignedConsumer : IConsumer<ReviewAssignedIntegrationEvent>
{
    private readonly INotificationOrchestrator _orchestrator;

    public ReviewAssignedConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<ReviewAssignedIntegrationEvent> context)
    {
        var e = context.Message;

        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.ReviewAssigned,
            Category: NotificationCategory.ReviewWorkflow,
            RecipientEmployeeId: e.ReviewerEmployeeId,
            RecipientEmail: e.ReviewerEmail,
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["ReviewerName"] = e.ReviewerDisplayName,
                ["RevieweeName"] = e.RevieweeDisplayName,
                ["CycleName"] = e.CycleName,
                ["Deadline"] = e.Deadline.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
