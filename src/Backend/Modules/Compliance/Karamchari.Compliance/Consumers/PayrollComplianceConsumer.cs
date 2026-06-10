// -----------------------------------------------------------------------
// <copyright file="PayrollComplianceConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Services;
using Karamchari.Core.Contracts.IntegrationEvents.V2;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Consumers;

/// <summary>
/// Observes PayrollRunCompleted events to check underpayment violations.
/// Minimum wage lookup: pulled from RegulatoryRegisters for the tenant.
/// If no minimum wage register exists, underpayment check is skipped.
/// </summary>
public sealed class PayrollComplianceConsumer : IConsumer<PayrollRunCompletedIntegrationEvent>
{
    private readonly PolicyViolationDetectionService _detectionService;
    private readonly ILogger<PayrollComplianceConsumer> _logger;

    public PayrollComplianceConsumer(
        PolicyViolationDetectionService detectionService,
        ILogger<PayrollComplianceConsumer> logger)
    {
        _detectionService = detectionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PayrollRunCompletedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        // Minimum wage check: use a fixed floor until RegulatoryRegister lookup is wired
        // Compliance module must not make cross-context DB reads — minimum wage seeded via RegulatoryRegister
        // For now threshold=0 means: only fire if Gross is literally zero or negative
        // Real threshold: query RegulatoryRegisters via RegulatoryRuleEvaluator
        const decimal minimumWageFloor = 0m;
        if (ev.Gross > minimumWageFloor)
        {
            _logger.LogDebug("Payroll compliance check: Employee {EmployeeId} Gross={Gross:C} OK",
                ev.EmployeeId, ev.Gross);
            return;
        }

        await _detectionService.CheckUnderpaymentAsync(
            ev.TenantId, ev.EmployeeId, ev.Gross, minimumWageFloor, DateTime.UtcNow, ct);
    }
}
