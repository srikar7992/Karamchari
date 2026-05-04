using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Core.Contracts;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using Karamchari.Payroll.Domain;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Consumer that handles the bulk calculation of payroll for all active profiles.
/// Implements the "Scatter" part of the Scatter-Gather pattern.
/// </summary>
public class CalculateAllEmployeePayConsumer : IConsumer<CalculateAllEmployeePayCommand>
{
    private readonly PayrollDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CalculateAllEmployeePayConsumer> _logger;
    private readonly StatutoryPipelineEngine _statutoryEngine;
    private readonly IProfessionalTaxProvider _ptProvider;
    private readonly IIncomeProjectionService _projectionService;
    private readonly IExemptionCalculator _exemptionCalculator;
    private readonly ITaxSlabProvider _taxSlabProvider;
    private readonly IITDeclarationRepository _declarationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculateAllEmployeePayConsumer"/> class.
    /// </summary>
    public CalculateAllEmployeePayConsumer(
        PayrollDbContext dbContext, 
        IPublishEndpoint publishEndpoint,
        ILogger<CalculateAllEmployeePayConsumer> logger,
        StatutoryPipelineEngine statutoryEngine,
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _statutoryEngine = statutoryEngine;
        _ptProvider = ptProvider;
        _projectionService = projectionService;
        _exemptionCalculator = exemptionCalculator;
        _taxSlabProvider = taxSlabProvider;
        _declarationRepository = declarationRepository;
    }

    /// <summary>
    /// Consumes the <see cref="CalculateAllEmployeePayCommand"/> and dispatches individual calculation events.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<CalculateAllEmployeePayCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Starting payroll calculation for Run {RunId}", message.RunId);
        }

        var activeProfiles = await _dbContext.PayrollProfiles
            .Where(p => p.IsActive)
            .ToListAsync(context.CancellationToken);

        foreach (var profile in activeProfiles)
        {
            decimal baseMonthlyGross = 0;
            if (profile.PayType == PayType.Hourly)
            {
                var timesheets = await _dbContext.TimesheetLedger
                    .Where(t => t.EmployeeId == profile.EmployeeId && !t.IsProcessed)
                    .ToListAsync(context.CancellationToken)
                    .ConfigureAwait(false);

                baseMonthlyGross = timesheets.Sum(t => t.TotalHours) * profile.HourlyRate;

                foreach (var t in timesheets) t.MarkAsProcessed();
            }
            else
            {
                baseMonthlyGross = profile.BaseSalary;
            }

            var externalDeductions = await _dbContext.PayrollDeductions
                .Where(d => d.EmployeeId == profile.EmployeeId && d.PeriodName == message.PeriodName && !d.IsProcessed)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            decimal totalExternalDeductions = externalDeductions.Sum(d => d.Amount);
            foreach (var d in externalDeductions) d.MarkAsProcessed();

            decimal finalMonthlyGross = baseMonthlyGross - totalExternalDeductions;

            // 4. Run Statutory Engine
            // Create a simple manual breakdown for now (Basic = 50%, HRA = 25%, Special = 25%)
            var monthlyBreakdown = new Dictionary<Guid, decimal>
            {
                { Guid.Parse("00000000-0000-0000-0000-000000000001"), Math.Round(finalMonthlyGross * 0.5m, 0) }, // Basic
                { Guid.Parse("00000000-0000-0000-0000-000000000002"), Math.Round(finalMonthlyGross * 0.25m, 0) }, // HRA
                { Guid.Parse("00000000-0000-0000-0000-000000000003"), Math.Round(finalMonthlyGross * 0.25m, 0) }  // Special
            };

            var adjustedBreakdown = new CTCBreakdownResult(
                AnnualCTC: finalMonthlyGross * 12,
                MonthlyGross: finalMonthlyGross,
                MonthlyBreakdown: monthlyBreakdown,
                AnnualBreakdown: monthlyBreakdown.ToDictionary(k => k.Key, v => v.Value * 12));

            var fy = new FinancialYear(2026, 2027);
            var ruleSet = new FY20262027RuleSet(
                new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") }, // Basic is PF base
                _ptProvider,
                _projectionService,
                _exemptionCalculator,
                _taxSlabProvider,
                _declarationRepository);

            var statutoryContext = new StatutoryContext(adjustedBreakdown, profile, fy, DateTime.UtcNow.Month);
            var statutoryResult = StatutoryPipelineEngine.Execute(statutoryContext, ruleSet);

            decimal netPay = statutoryResult.NetPay;

            var earningsMap = new Dictionary<string, decimal>
            {
                { "Basic", monthlyBreakdown[Guid.Parse("00000000-0000-0000-0000-000000000001")] },
                { "HRA", monthlyBreakdown[Guid.Parse("00000000-0000-0000-0000-000000000002")] },
                { "Special Allowance", monthlyBreakdown[Guid.Parse("00000000-0000-0000-0000-000000000003")] }
            };

            var ledgerEntry = PayrollLedgerEntry.Create(
                message.RunId,
                message.PeriodName,
                profile.EmployeeId,
                statutoryContext.PayrollMonth,
                DateTime.UtcNow.Year,
                fy.StartYear,
                finalMonthlyGross,
                statutoryResult.Deductions.GetValueOrDefault("TDS", 0),
                netPay,
                statutoryResult.Deductions,
                earningsMap);
            
            _dbContext.PayrollLedger.Add(ledgerEntry);

            // 8. Report back to the Saga (The "Gather" trigger)
            await _publishEndpoint.Publish(new EmployeePayCalculatedEvent(message.RunId, profile.EmployeeId, finalMonthlyGross, netPay), context.CancellationToken)
                .ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
