using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

public sealed class GoalApprovalRequiredConsumer : IConsumer<GoalApprovalRequiredIntegrationEvent>
{
    private readonly INotificationOrchestrator _orchestrator;

    public GoalApprovalRequiredConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public Task Consume(ConsumeContext<GoalApprovalRequiredIntegrationEvent> context)
    {
        var e = context.Message;

        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.GoalApprovalRequired,
            Category: NotificationCategory.GoalManagement,
            RecipientEmployeeId: e.ApproverManagerId,
            RecipientEmail: e.ApproverManagerEmail,
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["OwnerName"]  = e.OwnerDisplayName,
                ["GoalTitle"]  = e.GoalTitle,
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
