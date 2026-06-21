// -----------------------------------------------------------------------
// <copyright file="DisbursementConsumers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Services.Disbursement;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Initiates bank disbursement when command received from saga or API.
/// </summary>
public sealed class InitiateDisbursementConsumer : IConsumer<InitiateDisbursementCommand>
{
    private readonly BankDisbursementOrchestrator _orchestrator;
    private readonly ILogger<InitiateDisbursementConsumer> _logger;

    public InitiateDisbursementConsumer(
        BankDisbursementOrchestrator orchestrator,
        ILogger<InitiateDisbursementConsumer> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<InitiateDisbursementCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Initiating disbursement for run {RunId}, period {PeriodName}, bank {BankProvider}",
                msg.RunId, msg.PeriodName, msg.BankProvider);
        }

        if (!Enum.TryParse<Karamchari.Payroll.Domain.Disbursement.BankProvider>(
                msg.BankProvider, out var bankProvider))
            bankProvider = Karamchari.Payroll.Domain.Disbursement.BankProvider.Generic;

        var request = new DisbursementRequest(
            msg.TenantId, msg.RunId, msg.PeriodName,
            bankProvider, "", msg.InitiatedBy); // DebitAccountNumber

        var batch = await _orchestrator.InitiateAsync(request, context.CancellationToken);

        await context.Publish(new DisbursementBatchSubmittedIntegrationEventV1
        {
            BatchId = batch.Id,
            TenantId = msg.TenantId,
            RunId = msg.RunId,
            PeriodName = msg.PeriodName,
            TotalAmount = batch.TotalAmount,
            OccurredOnUtc = DateTimeOffset.UtcNow
        });
    }
}

/// <summary>
/// Handles disbursement retry requests.
/// </summary>
public sealed class RetryDisbursementConsumer : IConsumer<RetryDisbursementCommand>
{
    private readonly BankDisbursementOrchestrator _orchestrator;
    private readonly ILogger<RetryDisbursementConsumer> _logger;

    public RetryDisbursementConsumer(
        BankDisbursementOrchestrator orchestrator,
        ILogger<RetryDisbursementConsumer> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<RetryDisbursementCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Retrying disbursement batch {BatchId}", msg.BatchId);
        }

        await _orchestrator.SubmitAsync(msg.BatchId, "", context.CancellationToken); // DebitAccountNumber
    }
}
