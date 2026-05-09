using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Notifications.Orchestration;
using Karamchari.Notifications.Domain;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Routes payroll events to the notification orchestrator.
/// </summary>
public sealed class PayrollNotificationRouter :
    IConsumer<FnFSettlementApprovedIntegrationEvent>,
    IConsumer<FnFSettlementDisbursedIntegrationEvent>,
    IConsumer<DisbursementBatchCompletedIntegrationEvent>,
    IConsumer<DisbursementBatchFailedIntegrationEvent>,
    IConsumer<ArrearCalculationApprovedIntegrationEvent>,
    IConsumer<ReimbursementApprovedIntegrationEvent>,
    IConsumer<SalaryRevisionApprovedIntegrationEvent>
{
    private readonly INotificationOrchestrator _notifications;
    private readonly ILogger<PayrollNotificationRouter> _logger;

    public PayrollNotificationRouter(
        INotificationOrchestrator notifications,
        ILogger<PayrollNotificationRouter> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FnFSettlementApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _notifications.OrchestrateAsync(new NotificationIntent(
            context.Message.TenantId.ToString(),
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.FnFApproved,
            NotificationCategory.Payroll,
            context.Message.EmployeeId,
            $"employee_{context.Message.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["amount"] = context.Message.NetSettlementAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN")),
                ["approvedBy"] = context.Message.ApprovedBy
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<FnFSettlementDisbursedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _notifications.OrchestrateAsync(new NotificationIntent(
            context.Message.TenantId.ToString(),
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.FnFDisbursed,
            NotificationCategory.Payroll,
            context.Message.EmployeeId,
            $"employee_{context.Message.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["amount"] = context.Message.Amount.ToString("C", CultureInfo.GetCultureInfo("en-IN"))
            }), context.CancellationToken);
    }

    public Task Consume(ConsumeContext<DisbursementBatchCompletedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Disbursement batch {BatchId} completed. Success: {SuccessCount}, Failed: {FailedCount}",
                context.Message.BatchId, context.Message.SuccessCount, context.Message.FailedCount);
        }
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<DisbursementBatchFailedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_logger.IsEnabled(LogLevel.Error))
        {
            _logger.LogError(
                "Disbursement batch {BatchId} failed: {Reason}",
                context.Message.BatchId, context.Message.Reason);
        }
        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<ArrearCalculationApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _notifications.OrchestrateAsync(new NotificationIntent(
            context.Message.TenantId.ToString(),
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.ArrearApproved,
            NotificationCategory.Payroll,
            context.Message.EmployeeId,
            $"employee_{context.Message.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["delta"] = context.Message.TotalNetDelta.ToString("C", CultureInfo.GetCultureInfo("en-IN"))
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<ReimbursementApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _notifications.OrchestrateAsync(new NotificationIntent(
            context.Message.TenantId.ToString(),
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.ReimbursementApproved,
            NotificationCategory.Payroll,
            context.Message.EmployeeId,
            $"employee_{context.Message.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["amount"] = context.Message.ApprovedAmount.ToString("C", CultureInfo.GetCultureInfo("en-IN")),
                ["category"] = context.Message.Category
            }), context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<SalaryRevisionApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await _notifications.OrchestrateAsync(new NotificationIntent(
            context.Message.TenantId.ToString(),
            context.MessageId ?? Guid.NewGuid(),
            NotificationIntentType.SalaryRevisionApproved,
            NotificationCategory.Payroll,
            context.Message.EmployeeId,
            $"employee_{context.Message.EmployeeId}@karamchari.com",
            "en-IN",
            new Dictionary<string, string>
            {
                ["previousCTC"] = context.Message.PreviousCTC.ToString("C", CultureInfo.GetCultureInfo("en-IN")),
                ["newCTC"] = context.Message.NewCTC.ToString("C", CultureInfo.GetCultureInfo("en-IN")),
                ["effectiveFrom"] = context.Message.EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }), context.CancellationToken);
    }
}
