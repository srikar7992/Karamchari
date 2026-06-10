// -----------------------------------------------------------------------
// <copyright file="GoalCycleActivatedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Orchestration;
using Karamchari.Performance.Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Consumers;

/// <summary>
/// When a goal cycle activates, all employees should be prompted to set goals.
/// Phase 1: logs a system-level notification (no per-employee fan-out yet â€”
/// that requires an employee roster query which lives in the HR BC).
/// Phase 2: fetch active employee IDs from IOrganizationService and emit
/// one intent per employee (in batches to avoid notification storms).
/// </summary>
public sealed class GoalCycleActivatedConsumer : IConsumer<GoalCycleActivatedIntegrationEvent>
{
    private readonly INotificationOrchestrator _orchestrator;
    private readonly ILogger<GoalCycleActivatedConsumer> _logger;

    public GoalCycleActivatedConsumer(
        INotificationOrchestrator orchestrator,
        ILogger<GoalCycleActivatedConsumer> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Task Consume(ConsumeContext<GoalCycleActivatedIntegrationEvent> context)
    {
        var e = context.Message;

        // Phase 1: system alert only â€” HR is notified that the cycle activated.
        // Replace HR_SYSTEM_ID with a real lookup in Phase 2.
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "GoalCycleActivated consumed for cycle {CycleId} (tenant {TenantId}); " +
                "per-employee fan-out deferred to Phase 2",
                e.CycleId, e.TenantId);

        return Task.CompletedTask;
    }
}
