// -----------------------------------------------------------------------
// <copyright file="PayrollBatchConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using EFCore.BulkExtensions;
using Karamchari.Core.Contracts;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

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
    private readonly ITenantProvider _tenantProvider;

    public PayrollBatchConsumer(
        PayrollDbContext dbContext,
        ILogger<PayrollBatchConsumer> logger,
        IMemoryCache cache,
        IProfessionalTaxProvider ptProvider,
        IIncomeProjectionService projectionService,
        IExemptionCalculator exemptionCalculator,
        ITaxSlabProvider taxSlabProvider,
        IITDeclarationRepository declarationRepository,
        ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _cache = cache;
        _ptProvider = ptProvider;
        _projectionService = projectionService;
        _exemptionCalculator = exemptionCalculator;
        _taxSlabProvider = taxSlabProvider;
        _declarationRepository = declarationRepository;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task Consume(ConsumeContext<ProcessPayrollBatchCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var employeeIds = message.EmployeeIds;

        // Resolve tenant once per batch â€” scoped to this HTTP/message context.
        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Processing payroll batch of {Count} employees for Run {RunId}, Tenant {TenantId}",
                employeeIds.Count, message.RunId, tenantId);
        }

        // 0. Idempotency Check
        var processedEmployeeIds = await _dbContext.PayrollLedger
            .Where(e => e.RunId == message.RunId && employeeIds.Contains(e.EmployeeId))
            .Select(e => e.EmployeeId)
            .ToListAsync(context.CancellationToken);

        var pendingEmployeeIds = employeeIds.Except(processedEmployeeIds).ToList();

        if (pendingEmployeeIds.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("All employees in batch for Run {RunId} already processed. Skipping.", message.RunId);
            return;
        }

        // 1. Fetch data in bulk for the batch
        var profiles = await _dbContext.PayrollProfiles
            .Where(p => pendingEmployeeIds.Contains(p.EmployeeId))
            .ToListAsync(context.CancellationToken);

        // Cache keys are scoped per tenant to prevent cross-tenant data leaks in the
        // in-process IMemoryCache. Without the tenant prefix, tenant A's salary
        // components would be returned to tenant B on a cache hit.
        var componentsCacheKey = $"salary_components:{tenantId}";
        var templatesCacheKey = $"salary_templates:{tenantId}";

        var masterComponents = (await _cache.GetOrCreateAsync(componentsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _dbContext.SalaryComponents.ToListAsync(context.CancellationToken);
        }))!;

        var allTemplates = (await _cache.GetOrCreateAsync(templatesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _dbContext.SalaryTemplates.ToListAsync(context.CancellationToken);
        }))!;

        var results = new List<PayrollLedgerEntry>();
        var completionEvents = new List<EmployeePayCalculatedEvent>();

        // The rule set is loop-invariant for the batch (same FY, same providers), so build
        // it once. Pre-warm the PT slab cache for the year before the synchronous statutory
        // pipeline runs, so per-employee tax lookups resolve from cache instead of blocking
        // a thread-pool thread on a DB round-trip inside the sync path.
        var ruleSet = new FY20262027RuleSet(
            new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") },
            _ptProvider, _projectionService, _exemptionCalculator, _taxSlabProvider, _declarationRepository);
        await _ptProvider.PrimeAsync(ruleSet.Year, context.CancellationToken);

        foreach (var profile in profiles)
        {
            // For salaried employees use AnnualCTC (the agreed total cost) as the CTC breakdown
            // input â€” BaseSalary is the pre-template monthly floor used only for hourly fallback.
            // Passing BaseSalary * 12 would zero-out all template calculations for any profile
            // where BaseSalary has not yet been explicitly overridden (e.g. newly onboarded drafts).
            decimal annualCtc = profile.PayType == PayType.Hourly
                ? await GetHourlyGrossAsync(profile, context.CancellationToken) * 12
                : profile.AnnualCTC;

            var externalDeductions = await _dbContext.PayrollDeductions
                .Where(d => d.EmployeeId == profile.EmployeeId && d.PeriodName == message.PeriodName && !d.IsProcessed)
                .ToListAsync(context.CancellationToken);

            decimal totalExternalDeductions = externalDeductions.Sum(d => d.Amount);
            // External deductions (e.g. unpaid leave) reduce the monthly gross derived from the
            // AnnualCTC breakdown â€” they are not subtracted from the CTC input itself.
            decimal externalDeductionMonthly = totalExternalDeductions;

            var template = allTemplates.FirstOrDefault(t => t.Id == profile.SalaryTemplateId);
            if (template == null) continue;

            var plan = CTCTemplateCompiler.Compile(template, masterComponents);
            // Calculate the full CTC breakdown using the agreed annual CTC, then subtract
            // external per-period deductions from the derived monthly gross.
            var breakdown = CTCBreakdownService.Calculate(annualCtc, plan);
            decimal finalMonthlyGross = breakdown.MonthlyGross - externalDeductionMonthly;

            var statutoryContext = new StatutoryContext(breakdown, profile, ruleSet.Year, DateTime.UtcNow.Month);
            var statutoryResult = await StatutoryPipelineEngine.ExecuteAsync(statutoryContext, ruleSet);

            var earningsMap = breakdown.MonthlyBreakdown
                .ToDictionary(k => masterComponents.First(c => c.Id == k.Key).Name ?? string.Empty, v => v.Value);

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

        // 2. High-Performance Bulk Insert.
        // EFCore.BulkExtensions bypasses the EF Change Tracker and therefore also bypasses
        // TenantStampingInterceptor and RlsSessionContextInterceptor. We compensate by:
        //   a) Ensuring TenantId is stamped on each entry before insert (handled in
        //      PayrollLedgerEntry.Create via ITenantOwned â€” TenantStampingInterceptor will
        //      have already set SESSION_CONTEXT on the connection during the preceding queries
        //      in this scope, so RLS predicates remain active).
        //   b) Using SaveChangesAsync for the deduction MarkAsProcessed mutations below, which
        //      goes through the full interceptor chain and keeps session context consistent.
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
