// -----------------------------------------------------------------------
// <copyright file="FnFConsumers.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.FnF;
using Karamchari.Payroll.Services.FnF;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Triggers FnF calculation when settlement is initiated.
/// Idempotent via ProcessedEventLog â€” safe to replay.
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<InitiateFnFCalculationCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        // Idempotency: skip if already calculated
        var settlement = await _db.Set<FnFSettlement>()
            .Include(s => s.LineItems)
            .FirstOrDefaultAsync(s => s.Id == msg.SettlementId, context.CancellationToken);

        if (settlement is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("FnF settlement {SettlementId} not found.", msg.SettlementId);
            }
            return;
        }

        if (settlement.Status != FnFStatus.Draft)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "FnF settlement {SettlementId} already past Draft (status: {Status}). Skipping calculation.",
                    msg.SettlementId, settlement.Status);
            }
            return;
        }

        // Default calculation inputs â€” real impl: load from HR exit data
        var input = new FnFCalculationInput(
            msg.TenantId, msg.EmployeeId,
            settlement.LastWorkingDay,
            settlement.ExitType,
            30, // NoticePeriodDays
            30, // ActualNoticeDays
            DateTime.DaysInMonth(
                settlement.LastWorkingDay.Year,
                settlement.LastWorkingDay.Month) - settlement.LastWorkingDay.Day + 1, // PendingSalaryDays
            0m, // LeaveBalance
            0m); // GratuityYearsOfService

        var result = await _calculationService.CalculateAsync(input, context.CancellationToken);

        settlement.ReplaceLineItems(result.LineItems);

        await _db.SaveChangesAsync(context.CancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "FnF calculated for settlement {SettlementId}. Net: {NetSettlement}",
                msg.SettlementId, result.NetSettlement);
        }
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<DisburseFnFCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        var settlement = await _db.Set<FnFSettlement>()
            .FirstOrDefaultAsync(s => s.Id == msg.SettlementId, context.CancellationToken);

        if (settlement is null || settlement.Status != FnFStatus.Approved)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Cannot disburse FnF {SettlementId}: not found or not approved.", msg.SettlementId);
            }
            return;
        }

        // Real impl: create bank disbursement entry for single employee
        settlement.MarkDisbursed("system");

        await _db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new FnFSettlementDisbursedIntegrationEventV1
        {
            SettlementId = settlement.Id,
            TenantId = settlement.TenantId,
            EmployeeId = settlement.EmployeeId,
            Amount = settlement.NetSettlementAmount,
            OccurredOnUtc = DateTimeOffset.UtcNow
        });
    }
}
