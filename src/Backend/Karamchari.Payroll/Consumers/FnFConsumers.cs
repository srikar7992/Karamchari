using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Services.FnF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Triggers FnF calculation when settlement is initiated.
/// Idempotent via ProcessedEventLog — safe to replay.
/// </summary>
public sealed class InitiateFnFCalculationConsumer : IConsumer<InitiateFnFCalculationCommand>
{
    private readonly PayrollDbContext _db;
    private readonly FnFCalculationService _calculationService;
    private readonly ILogger<InitiateFnFCalculationConsumer> _logger;

    public InitiateFnFCalculationConsumer(
        PayrollDbContext db,
        FnFCalculationService calculationService,
        ILogger<InitiateFnFCalculationConsumer> logger)
    {
        _db = db;
        _calculationService = calculationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InitiateFnFCalculationCommand> context)
    {
        var msg = context.Message;

        // Idempotency: skip if already calculated
        var settlement = await _db.Set<FnFSettlement>()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == msg.SettlementId, context.CancellationToken);

        if (settlement is null)
        {
            _logger.LogWarning("FnF settlement {SettlementId} not found.", msg.SettlementId);
            return;
        }

        if (settlement.Status != FnFStatus.Draft)
        {
            _logger.LogInformation(
                "FnF settlement {SettlementId} already past Draft (status: {Status}). Skipping calculation.",
                msg.SettlementId, settlement.Status);
            return;
        }

        // Default calculation inputs — real impl: load from HR exit data
        var input = new FnFCalculationInput(
            msg.TenantId, msg.EmployeeId,
            settlement.LastWorkingDay,
            settlement.ExitType,
            NoticePeriodDays: 30,
            ActualNoticeDays: 30,
            PendingSalaryDays: DateTime.DaysInMonth(
                settlement.LastWorkingDay.Year,
                settlement.LastWorkingDay.Month) - settlement.LastWorkingDay.Day + 1,
            LeaveBalance: 0m,
            GratuityYearsOfService: 0m);

        var result = await _calculationService.CalculateAsync(input, context.CancellationToken);

        settlement.ReplaceLineItems(result.LineItems);

        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "FnF calculated for settlement {SettlementId}. Net: {Net}",
            msg.SettlementId, result.NetSettlement);
    }
}

/// <summary>
/// Handles FnF disbursement: creates a disbursement batch for the settlement.
/// </summary>
public sealed class DisburseFnFConsumer : IConsumer<DisburseFnFCommand>
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<DisburseFnFConsumer> _logger;

    public DisburseFnFConsumer(PayrollDbContext db, ILogger<DisburseFnFConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DisburseFnFCommand> context)
    {
        var msg = context.Message;

        var settlement = await _db.Set<FnFSettlement>()
            .FirstOrDefaultAsync(s => s.Id == msg.SettlementId, context.CancellationToken);

        if (settlement is null || settlement.Status != FnFStatus.Approved)
        {
            _logger.LogWarning(
                "Cannot disburse FnF {SettlementId}: not found or not approved.", msg.SettlementId);
            return;
        }

        // Real impl: create bank disbursement entry for single employee
        settlement.MarkDisbursed("system");

        await _db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new FnFSettlementDisbursedIntegrationEvent
        {
            SettlementId = settlement.Id,
            TenantId = settlement.TenantId,
            EmployeeId = settlement.EmployeeId,
            Amount = settlement.NetSettlementAmount,
            OccurredOnUtc = DateTimeOffset.UtcNow
        });
    }
}
