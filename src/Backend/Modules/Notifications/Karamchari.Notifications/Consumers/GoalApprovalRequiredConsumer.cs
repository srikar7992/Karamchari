// -----------------------------------------------------------------------
// <copyright file="GoalApprovalRequiredConsumer.cs" company="Karamchari">
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
/// Provides required documentation for this member.
/// </summary>
public sealed class GoalApprovalRequiredConsumer : IConsumer<GoalApprovalRequiredIntegrationEventV1>
{
    private readonly INotificationOrchestrator _orchestrator;

    public GoalApprovalRequiredConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<GoalApprovalRequiredIntegrationEventV1> context)
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
                ["OwnerName"] = e.OwnerDisplayName,
                ["GoalTitle"] = e.GoalTitle,
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
