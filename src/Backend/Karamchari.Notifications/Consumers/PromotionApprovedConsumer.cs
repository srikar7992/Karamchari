using System.Globalization;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

public sealed class PromotionApprovedConsumer : IConsumer<PromotionApprovedIntegrationEvent>
{
    private readonly INotificationOrchestrator _orchestrator;

    public PromotionApprovedConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public Task Consume(ConsumeContext<PromotionApprovedIntegrationEvent> context)
    {
        var e = context.Message;

        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.PromotionApproved,
            Category: NotificationCategory.PromotionApprovals,
            RecipientEmployeeId: e.EmployeeId,
            RecipientEmail: string.Empty,
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["PreviousTitle"]   = e.PreviousTitle,
                ["NewTitle"]        = e.NewTitle,
                ["PreviousGrade"]   = e.PreviousGrade,
                ["NewGrade"]        = e.NewGrade,
                ["EffectiveDate"]   = e.EffectiveDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
