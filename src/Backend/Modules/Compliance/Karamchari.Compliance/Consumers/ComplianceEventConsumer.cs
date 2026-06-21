// -----------------------------------------------------------------------
// <copyright file="ComplianceEventConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Services;
using Karamchari.Core.Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Consumers;

/// <summary>
/// Observes upstream domain events and routes to the violation detection engine.
/// Compliance never modifies source systems — observe, evaluate, detect only.
///
/// Currently handled:
///   TimesheetApprovedIntegrationEventV1 -> ExcessiveOvertime check (> 60h/week)
///
/// Future: PayrollCalculated -> Underpayment check (requires minimum wage lookup per tenant/site).
/// PayrollCalculatedIntegrationEvent is not defined in contracts; skipped until published.
/// </summary>
public sealed class ComplianceEventConsumer :
    IConsumer<TimesheetApprovedIntegrationEventV1>
{
    private readonly PolicyViolationDetectionService _detectionService;
    private readonly ILogger<ComplianceEventConsumer> _logger;

    public ComplianceEventConsumer(
        PolicyViolationDetectionService detectionService,
        ILogger<ComplianceEventConsumer> logger)
    {
        _detectionService = detectionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEventV1> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        await _detectionService.CheckExcessiveOvertimeAsync(
            ev.TenantId, ev.EmployeeId, ev.TotalHours, ev.WeekStartDate, ct);

        _logger.LogDebug("Compliance check on timesheet: Employee {EmployeeId} {Hours:F1}h",
            ev.EmployeeId, ev.TotalHours);
    }
}
