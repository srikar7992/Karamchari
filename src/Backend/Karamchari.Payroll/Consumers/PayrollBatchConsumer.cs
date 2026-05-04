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
using EFCore.BulkExtensions;
using Microsoft.Extensions.Caching.Memory;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Processes a batch of employees, calculating their payroll and performing bulk inserts.
/// This optimizes performance for large payroll runs (10k+ employees).
/// </summary>
public sealed class PayrollBatchConsumer : IConsumer<ProcessPayrollBatchCommand>
{
    private readonly PayrollDbContext _dbContext;
    private readonly ILogger<PayrollBatchConsumer> _logger;
    private readonly IMemoryCache _cache;
    private readonly IProfessionalTaxProvider _ptProvider;
    private readonly IIncomeProjectionService _projectionService;
    private readonly IExemptionCalculator _exemptionCalculator;
    private readonly ITaxSlabProvider _taxSlabProvider;
    private readonly IITDeclarationRepository _declarationRepository;

    public PayrollBatchConsumer(
        PayrollDbContext dbContext,
        ILogger<PayrollBatchConsumer> logger,
        IMemoryCache cache,
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _ptProvider = ptProvider;
        _projectionService = projectionService;
        _exemptionCalculator = exemptionCalculator;
        _taxSlabProvider = taxSlabProvider;
        _declarationRepository = declarationRepository;
    }

    public async Task Consume(ConsumeContext<ProcessPayrollBatchCommand> context)
    {
        var message = context.Message;
        var employeeIds = message.EmployeeIds;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Processing payroll batch of {Count} employees for Run {RunId}", employeeIds.Count, message.RunId);
        }

        // 0. Idempotency Check
        var processedEmployeeIds = await _dbContext.PayrollLedger
            .Where(e => e.RunId == message.RunId && employeeIds.Contains(e.EmployeeId))
            .Select(e => e.EmployeeId)
            .ToListAsync(context.CancellationToken);

        var pendingEmployeeIds = employeeIds.Except(processedEmployeeIds).ToList();

        if (!pendingEmployeeIds.Any())
        {
            _logger.LogInformation("All employees in batch for Run {RunId} already processed. Skipping.", message.RunId);
            return;
        }

        // 1. Fetch data in bulk for the batch
        var profiles = await _dbContext.PayrollProfiles
            .Where(p => pendingEmployeeIds.Contains(p.EmployeeId))
            .ToListAsync(context.CancellationToken);

        // Cache-optimized fetching of master components and templates
        var masterComponents = await _cache.GetOrCreateAsync("master_salary_components", async entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _dbContext.SalaryComponents.ToListAsync(context.CancellationToken);
        }) ?? new List<SalaryComponent>();

        var allTemplates = await _cache.GetOrCreateAsync("salary_templates", async entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _dbContext.SalaryTemplates.ToListAsync(context.CancellationToken);
        }) ?? new List<SalaryTemplate>();

        var results = new List<PayrollLedgerEntry>();
        var completionEvents = new List<EmployeePayCalculatedEvent>();

        foreach (var profile in profiles)
        {
            // Calculation logic (same as before, but optimized with local lookups)
            decimal baseMonthlyGross = profile.PayType == PayType.Hourly 
                ? await GetHourlyGrossAsync(profile, context.CancellationToken)
                : profile.BaseSalary;

            var externalDeductions = await _dbContext.PayrollDeductions
                .Where(d => d.EmployeeId == profile.EmployeeId && d.PeriodName == message.PeriodName && !d.IsProcessed)
                .ToListAsync(context.CancellationToken);

            decimal totalExternalDeductions = externalDeductions.Sum(d => d.Amount);
            decimal finalMonthlyGross = baseMonthlyGross - totalExternalDeductions;

            var template = allTemplates.FirstOrDefault(t => t.Id == profile.SalaryTemplateId);
            if (template == null) continue;

            var plan = CTCTemplateCompiler.Compile(template, masterComponents);
            var breakdown = CTCBreakdownService.Calculate(finalMonthlyGross * 12, plan);

            var ruleSet = new FY20262027RuleSet(
                new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") },
                _ptProvider, _projectionService, _exemptionCalculator, _taxSlabProvider, _declarationRepository);

            var statutoryContext = new StatutoryContext(breakdown, profile, ruleSet.Year, DateTime.UtcNow.Month);
            var statutoryResult = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);

            var earningsMap = breakdown.MonthlyBreakdown
                .ToDictionary(k => masterComponents.First(c => c.Id == k.Key).Name, v => v.Value);

            var ledgerEntry = PayrollLedgerEntry.Create(
                message.RunId, message.PeriodName, profile.EmployeeId, 
                statutoryContext.PayrollMonth, DateTime.UtcNow.Year, ruleSet.Year.StartYear,
                finalMonthlyGross, statutoryResult.Deductions.GetValueOrDefault("TDS", 0),
                statutoryResult.NetPay, statutoryResult.Deductions, earningsMap);

            results.Add(ledgerEntry);
            completionEvents.Add(new EmployeePayCalculatedEvent(message.RunId, profile.EmployeeId, finalMonthlyGross, statutoryResult.NetPay));
            
            // Mark deductions as processed (In bulk later if possible, but for now simple)
            foreach (var d in externalDeductions) d.MarkAsProcessed();
        }

        // 2. High-Performance Bulk Insert
        await _dbContext.BulkInsertAsync(results, cancellationToken: context.CancellationToken);

        // 3. Batch reporting back to Saga
        // Note: MassTransit handles publishing multiple events efficiently
        await Task.WhenAll(completionEvents.Select(e => context.Publish(e)));

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private async Task<decimal> GetHourlyGrossAsync(PayrollProfile profile, CancellationToken ct)
    {
        var timesheets = await _dbContext.TimesheetLedger
            .Where(t => t.EmployeeId == profile.EmployeeId && !t.IsProcessed)
            .ToListAsync(ct);

        decimal gross = timesheets.Sum(t => t.TotalHours) * profile.HourlyRate;
        foreach (var t in timesheets) t.MarkAsProcessed();
        return gross;
    }
}
