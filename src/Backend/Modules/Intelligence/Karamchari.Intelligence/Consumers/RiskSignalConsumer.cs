// -----------------------------------------------------------------------
// <copyright file="RiskSignalConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Contracts;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Consumers;

/// <summary>
/// Consumes <see cref="RiskSignalRaisedIntegrationEvent"/> from any upstream source
/// (burnout engine, attrition engine, external AI, performance engine, etc.).
///
/// By consuming a canonical cross-source event the Intelligence module stays decoupled
/// from signal producers — it does not need to know whether a signal came from the
/// internal burnout calculator or an external ML service.
/// </summary>
public sealed class RiskSignalConsumer : IConsumer<RiskSignalRaisedIntegrationEvent>
{
    private readonly WorkforceSignalService _signalService;
    private readonly ILogger<RiskSignalConsumer> _logger;

    public RiskSignalConsumer(
        WorkforceSignalService signalService,
        ILogger<RiskSignalConsumer> logger)
    {
        _signalService = signalService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RiskSignalRaisedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        if (!Enum.TryParse<WorkforceSignalType>(ev.SignalType, ignoreCase: true, out var signalType))
        {
            _logger.LogWarning(
                "RiskSignalRaisedIntegrationEvent from {Source} contained unknown SignalType '{Type}' for employee {EmployeeId} — skipped",
                ev.SourceModule, ev.SignalType, ev.EmployeeId);
            return;
        }

        await _signalService.UpsertSignalAsync(
            ev.TenantId,
            ev.EmployeeId,
            signalType,
            ev.SignalValue,
            ev.SignalDate,
            ct);

        await _signalService.RecalculateEmployeeAsync(ev.TenantId, ev.EmployeeId, ct: ct);

        _logger.LogDebug(
            "RiskSignal [{Type}={Value}] from {Source} applied for employee {EmployeeId} (correlation {CorrelationId})",
            signalType, ev.SignalValue, ev.SourceModule, ev.EmployeeId, ev.CorrelationId);
    }
}
