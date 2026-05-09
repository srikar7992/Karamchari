using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Arrears;
using Karamchari.Payroll.Domain.SalaryRevisions;
using Karamchari.Payroll.Services.Arrears;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Triggers arrear calculation when a salary revision is approved.
/// Only fires if revision requires arrears (effective date in past processed period).
/// </summary>
public sealed class SalaryRevisionApprovedArrearConsumer : IConsumer<SalaryRevisionApprovedIntegrationEvent>
{
    private readonly ArrearCalculationEngine _engine;
    private readonly PayrollDbContext _db;
    private readonly ILogger<SalaryRevisionApprovedArrearConsumer> _logger;

    public SalaryRevisionApprovedArrearConsumer(
        ArrearCalculationEngine engine,
        PayrollDbContext db,
        ILogger<SalaryRevisionApprovedArrearConsumer> logger)
    {
        _engine = engine;
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SalaryRevisionApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        if (!msg.RequiresArrears)
            return;

        // Check if arrear already computed for this revision (idempotency)
        var existing = await _db.Set<ArrearCalculation>()
            .AnyAsync(a => a.TriggerReference == msg.RevisionId.ToString(), context.CancellationToken);

        if (existing)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Arrear already computed for revision {RevisionId}. Skipping.", msg.RevisionId);
            }
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new ArrearCalculationRequest(
            msg.TenantId,
            msg.EmployeeId,
            "", // EmployeeName
            ArrearTriggerType.SalaryRevision,
            msg.RevisionId.ToString(),
            msg.EffectiveFrom,
            today.AddMonths(-1),
            "system"); // InitiatedBy

        var result = await _engine.ComputeAsync(request, context.CancellationToken);

        if (result.PeriodDiffs.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "No arrear differentials found for revision {RevisionId}.", msg.RevisionId);
            }
            return;
        }

        result.Calculation.SubmitForApproval();

        _db.Set<ArrearCalculation>().Add(result.Calculation);
        await _db.SaveChangesAsync(context.CancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Arrear calculation {ArrearId} created for revision {RevisionId}. Delta: {TotalNetDelta}",
                result.Calculation.Id, msg.RevisionId, result.Calculation.TotalNetDelta);
        }
    }
}

/// <summary>
/// Executes arrear processing when approved: injects arrear into next payroll run.
/// </summary>
public sealed class ArrearApprovedConsumer : IConsumer<ArrearCalculationApprovedIntegrationEvent>
{
    private readonly PayrollDbContext _db;
    private readonly ILogger<ArrearApprovedConsumer> _logger;

    public ArrearApprovedConsumer(PayrollDbContext db, ILogger<ArrearApprovedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ArrearCalculationApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        var arrear = await _db.Set<ArrearCalculation>()
            .FirstOrDefaultAsync(a => a.Id == msg.ArrearId, context.CancellationToken);

        if (arrear is null || arrear.Status != ArrearStatus.Approved)
            return;

        // Create payroll deduction record for arrear payout in next cycle
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentPeriod = $"{today.Year}-{today.Month:D2}";

        var deduction = Karamchari.Payroll.Domain.PayrollDeduction.Create(
            msg.TenantId.ToString(), msg.EmployeeId, currentPeriod,
            msg.TotalNetDelta,
            $"Arrear payout for {arrear.TriggerType} (ID: {msg.ArrearId})");

        _db.PayrollDeductions.Add(deduction);
        arrear.MarkProcessed(currentPeriod, runId: Guid.NewGuid().ToString());
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
