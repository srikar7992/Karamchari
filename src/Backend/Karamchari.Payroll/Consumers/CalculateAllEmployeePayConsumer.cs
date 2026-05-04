using MassTransit;
using Karamchari.Payroll.Contracts;
using Karamchari.Payroll.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculateAllEmployeePayConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The payroll database context.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="logger">The logger.</param>
    public CalculateAllEmployeePayConsumer(
        PayrollDbContext dbContext, 
        IPublishEndpoint publishEndpoint,
        ILogger<CalculateAllEmployeePayConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
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

        // 1. Fetch all active payroll profiles for the tenant
        // (RLS automatically filters this to the correct tenant)
        var activeProfiles = await _dbContext.PayrollProfiles
            .Where(p => p.IsActive)
            .ToListAsync(context.CancellationToken);

        if (activeProfiles.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("No active payroll profiles found for Run {RunId}. Calculation aborted.", message.RunId);
            }
            return; 
        }

        // 2. The Loop (The "Scatter")
        foreach (var profile in activeProfiles)
        {
            // 2a. Calculate Gross Pay based on PayType
            decimal grossPay = 0;
            if (profile.PayType == Domain.PayType.Hourly)
            {
                // Fetch all approved, unprocessed hours for this employee
                var timesheets = await _dbContext.TimesheetLedger
                    .Where(t => t.EmployeeId == profile.EmployeeId && !t.IsProcessed)
                    .ToListAsync(context.CancellationToken)
                    .ConfigureAwait(false);

                grossPay = timesheets.Sum(t => t.TotalHours) * profile.HourlyRate;

                // Mark hours as processed to prevent double-pay in future runs
                foreach (var t in timesheets)
                {
                    t.MarkAsProcessed();
                }
            }
            else
            {
                grossPay = profile.BaseSalary;
            }

            // 3. Apply Deductions (Unpaid leaves, etc.)
            var deductions = await _dbContext.PayrollDeductions
                .Where(d => d.EmployeeId == profile.EmployeeId && d.PeriodName == message.PeriodName && !d.IsProcessed)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            decimal totalDeductions = deductions.Sum(d => d.Amount);
            decimal netPay = grossPay - totalDeductions;

            // Mark deductions as processed for this run
            foreach (var d in deductions)
            {
                d.MarkAsProcessed();
            }

            // 3. Report back to the Saga (The "Gather" trigger)
            await _publishEndpoint.Publish(new EmployeePayCalculatedEvent(message.RunId, profile.EmployeeId, netPay), context.CancellationToken)
                .ConfigureAwait(false);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Dispatched calculation events for {Count} employees in Run {RunId}", activeProfiles.Count, message.RunId);
        }
    }
}
