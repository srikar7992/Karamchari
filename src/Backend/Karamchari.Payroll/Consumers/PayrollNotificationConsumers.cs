using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Notifications.Orchestration;
using Microsoft.Extensions.Logging;

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

    public Task Consume(ConsumeContext<FnFSettlementApprovedIntegrationEvent> context)
        => _notifications.SendAsync(new NotificationIntent
        {
            TenantId = context.Message.TenantId,
            RecipientEmployeeId = context.Message.EmployeeId,
            Type = NotificationIntentType.FnFApproved,
            TemplateData = new Dictionary<string, string>
            {
                ["amount"] = context.Message.NetSettlementAmount.ToString("C"),
                ["approvedBy"] = context.Message.ApprovedBy
            }
        }, context.CancellationToken);

    public Task Consume(ConsumeContext<FnFSettlementDisbursedIntegrationEvent> context)
        => _notifications.SendAsync(new NotificationIntent
        {
            TenantId = context.Message.TenantId,
            RecipientEmployeeId = context.Message.EmployeeId,
            Type = NotificationIntentType.FnFDisbursed,
            TemplateData = new Dictionary<string, string>
            {
                ["amount"] = context.Message.Amount.ToString("C")
            }
        }, context.CancellationToken);

    public Task Consume(ConsumeContext<DisbursementBatchCompletedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "Disbursement batch {BatchId} completed. Success: {S}, Failed: {F}",
            context.Message.BatchId, context.Message.SuccessCount, context.Message.FailedCount);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<DisbursementBatchFailedIntegrationEvent> context)
    {
        _logger.LogError(
            "Disbursement batch {BatchId} failed: {Reason}",
            context.Message.BatchId, context.Message.Reason);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<ArrearCalculationApprovedIntegrationEvent> context)
        => _notifications.SendAsync(new NotificationIntent
        {
            TenantId = context.Message.TenantId,
            RecipientEmployeeId = context.Message.EmployeeId,
            Type = NotificationIntentType.ArrearApproved,
            TemplateData = new Dictionary<string, string>
            {
                ["delta"] = context.Message.TotalNetDelta.ToString("C")
            }
        }, context.CancellationToken);

    public Task Consume(ConsumeContext<ReimbursementApprovedIntegrationEvent> context)
        => _notifications.SendAsync(new NotificationIntent
        {
            TenantId = context.Message.TenantId,
            RecipientEmployeeId = context.Message.EmployeeId,
            Type = NotificationIntentType.ReimbursementApproved,
            TemplateData = new Dictionary<string, string>
            {
                ["amount"] = context.Message.ApprovedAmount.ToString("C"),
                ["category"] = context.Message.Category
            }
        }, context.CancellationToken);

    public Task Consume(ConsumeContext<SalaryRevisionApprovedIntegrationEvent> context)
        => _notifications.SendAsync(new NotificationIntent
        {
            TenantId = context.Message.TenantId,
            RecipientEmployeeId = context.Message.EmployeeId,
            Type = NotificationIntentType.SalaryRevisionApproved,
            TemplateData = new Dictionary<string, string>
            {
                ["previousCTC"] = context.Message.PreviousCTC.ToString("C"),
                ["newCTC"] = context.Message.NewCTC.ToString("C"),
                ["effectiveFrom"] = context.Message.EffectiveFrom.ToString("yyyy-MM-dd")
            }
        }, context.CancellationToken);
}
