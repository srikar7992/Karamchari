using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Services.Disbursement;
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

    public async Task Consume(ConsumeContext<InitiateDisbursementCommand> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Initiating disbursement for run {RunId}, period {Period}, bank {Bank}",
            msg.RunId, msg.PeriodName, msg.BankProvider);

        if (!Enum.TryParse<Karamchari.Payroll.Domain.Disbursement.BankProvider>(
                msg.BankProvider, out var bankProvider))
            bankProvider = Karamchari.Payroll.Domain.Disbursement.BankProvider.Generic;

        var request = new DisbursementRequest(
            msg.TenantId, msg.RunId, msg.PeriodName,
            bankProvider, debitAccountNumber: "", msg.InitiatedBy);

        var batch = await _orchestrator.InitiateAsync(request, context.CancellationToken);

        await context.Publish(new DisbursementBatchSubmittedIntegrationEvent
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

    public async Task Consume(ConsumeContext<RetryDisbursementCommand> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Retrying disbursement batch {BatchId}", msg.BatchId);
        await _orchestrator.SubmitAsync(msg.BatchId, debitAccountNumber: "", context.CancellationToken);
    }
}
