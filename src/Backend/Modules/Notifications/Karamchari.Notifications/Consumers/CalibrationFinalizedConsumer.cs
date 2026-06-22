// -----------------------------------------------------------------------
// <copyright file="CalibrationFinalizedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// Notifies the calibrated employee that their calibration result is finalized.
/// Phase 1: notification to employee. Phase 2 will also notify their manager.
/// </summary>
public sealed class CalibrationFinalizedConsumer : IConsumer<EmployeeCalibrationFinalizedIntegrationEventV1>
{
    private readonly INotificationOrchestrator _orchestrator;

    public CalibrationFinalizedConsumer(INotificationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<EmployeeCalibrationFinalizedIntegrationEventV1> context)
    {
        var e = context.Message;

        var intent = new NotificationIntent(
            TenantId: e.TenantId,
            TriggerEventId: context.CorrelationId ?? Guid.NewGuid(),
            IntentType: NotificationIntentType.CalibrationFinalized,
            Category: NotificationCategory.CalibrationActions,
            RecipientEmployeeId: e.EmployeeId,
            RecipientEmail: string.Empty,   // email resolved at preference layer; no email in this event
            Locale: "en-US",
            Variables: new Dictionary<string, string>
            {
                ["PerformanceBucket"] = e.PerformanceBucket,
                ["CalibratedScore"] = e.CalibratedScore.ToString("F1", CultureInfo.InvariantCulture),
            });

        return _orchestrator.OrchestrateAsync(intent, context.CancellationToken);
    }
}
