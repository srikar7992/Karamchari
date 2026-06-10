// -----------------------------------------------------------------------
// <copyright file="CalculateAllEmployeePayConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services;
using Karamchari.Payroll.Services.Statutory;
using Karamchari.Payroll.Services.Statutory.Rules;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Consumer that handles the bulk calculation of payroll for all active profiles.
/// Implements the "Scatter" part of the Scatter-Gather pattern.
///
/// Resumability: on restart, already-calculated employees (present in EmployeePayrollResults
/// or PayrollLedger for the same RunId) are excluded from new batches so no employee is
/// calculated twice and the run continues from where it left off.
/// </summary>
public class CalculateAllEmployeePayCommandConsumer : IConsumer<CalculateAllEmployeePayCommand>
{
    private readonly PayrollDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CalculateAllEmployeePayCommandConsumer> _logger;
    private readonly StatutoryPipelineEngine _statutoryEngine;
    private readonly IProfessionalTaxProvider _ptProvider;
    private readonly IIncomeProjectionService _projectionService;
    private readonly IExemptionCalculator _exemptionCalculator;
    private readonly ITaxSlabProvider _taxSlabProvider;
    private readonly IITDeclarationRepository _declarationRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculateAllEmployeePayCommandConsumer"/> class.
    /// </summary>
    public CalculateAllEmployeePayCommandConsumer(
        PayrollDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILogger<CalculateAllEmployeePayCommandConsumer> logger,
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

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CalculateAllEmployeePayCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;

        // 1. Fetch all active employee IDs for the current tenant
        var allEmployeeIds = await _dbContext.PayrollProfiles
            .Where(p => p.IsActive)
            .Select(p => p.EmployeeId)
            .ToListAsync(context.CancellationToken);

        // 2. Resume support: exclude employees already calculated for this run.
        //    Checks both Phase 4 EmployeePayrollResults and legacy PayrollLedger so the
        //    scatter is safe to re-deliver (e.g. after worker crash or saga retry).
        var alreadyDonePhase4 = await _dbContext.EmployeePayrollResults
            .AsNoTracking()
            .Where(r => r.PayrollRunId == message.RunId)
            .Select(r => r.EmployeeId)
            .ToListAsync(context.CancellationToken);

        var alreadyDoneLegacy = await _dbContext.PayrollLedger
            .AsNoTracking()
            .Where(e => e.RunId == message.RunId)
            .Select(e => e.EmployeeId)
            .ToListAsync(context.CancellationToken);

        var alreadyDone = alreadyDonePhase4.Union(alreadyDoneLegacy).ToHashSet();
        var pendingIds = allEmployeeIds.Where(id => !alreadyDone.Contains(id)).ToList();

        if (pendingIds.Count == 0)
        {
            _logger.LogInformation(
                "Run {RunId}: all {Total} employees already calculated. Scatter skipped (resume complete).",
                message.RunId, allEmployeeIds.Count);
            return;
        }

        if (alreadyDone.Count > 0)
        {
            _logger.LogInformation(
                "Run {RunId}: resuming — {Done} already done, {Pending} remaining.",
                message.RunId, alreadyDone.Count, pendingIds.Count);
        }
        else
        {
            _logger.LogInformation(
                "Run {RunId}: starting fresh — {Count} employees.",
                message.RunId, pendingIds.Count);
        }

        // 3. Chunk pending employees into batches (100 per batch)
        var batches = pendingIds.Chunk(100);

        foreach (var batch in batches)
        {
            await context.Publish(new ProcessPayrollBatchCommand(
                message.RunId,
                message.PeriodName,
                batch.ToList()));
        }
    }
}
