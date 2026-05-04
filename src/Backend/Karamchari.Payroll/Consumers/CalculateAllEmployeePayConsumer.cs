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
public class CalculateAllEmployeePayCommandConsumer : IConsumer<CalculateAllEmployeePayCommand>
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

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CalculateAllEmployeePayCommand> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        
        // 1. Fetch all active employee IDs for the current tenant
        var employeeIds = await _dbContext.PayrollProfiles
            .Where(p => p.IsActive)
            .Select(p => p.EmployeeId)
            .ToListAsync(context.CancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Dispatching payroll batches for Run {RunId}. Total employees: {Count}", message.RunId, employeeIds.Count);
        }

        // 2. Chunk into batches (Pattern: 100 employees per batch)
        var batches = employeeIds.Chunk(100);

        foreach (var batch in batches)
        {
            await context.Publish(new ProcessPayrollBatchCommand(
                message.RunId,
                message.PeriodName,
                batch.ToList()));
        }
    }
}
