// -----------------------------------------------------------------------
// <copyright file="ReviewSubmittedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// Notifies HR when any review is submitted â€” useful for cycle completion tracking.
/// In a full implementation the manager of the reviewee would also be notified.
/// Phase 1: HR system-alert only (HR employee ID sourced from a future
/// TenantConfiguration BC; stub uses Guid.Empty which a real template will skip).
/// </summary>
public sealed class ReviewSubmittedConsumer : IConsumer<ReviewSubmittedIntegrationEventV1>
{
    private readonly INotificationOrchestrator _orchestrator;

    public ReviewSubmittedConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<ReviewSubmittedIntegrationEventV1> context)
    {
        var e = context.Message;

        // Phase 1: emit back to the reviewer as a submission confirmation.
        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.ReviewSubmitted,
            Category: NotificationCategory.ReviewWorkflow,
            RecipientEmployeeId: e.ReviewerEmployeeId,
            RecipientEmail: string.Empty,   // reviewer email not in this event â€” template skips email channel
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["RevieweeName"] = e.RevieweeDisplayName,
                ["CycleName"] = e.CycleName,
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
