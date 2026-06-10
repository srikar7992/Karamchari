// -----------------------------------------------------------------------
// <copyright file="EmployeeOnboardedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Payroll.Consumers;

/// <summary>
/// Consumer for the <see cref="EmployeeOnboardedIntegrationEvent"/>.
/// Creates a draft payroll profile when a new employee is onboarded.
/// </summary>
public class EmployeeOnboardedConsumer : IConsumer<EmployeeOnboardedIntegrationEvent>
{
    private readonly PayrollDbContext _dbContext;
    private readonly ILogger<EmployeeOnboardedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeOnboardedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The payroll database context.</param>
    /// <param name="logger">The logger.</param>
    public EmployeeOnboardedConsumer(PayrollDbContext dbContext, ILogger<EmployeeOnboardedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the <see cref="EmployeeOnboardedIntegrationEvent"/>.
    /// </summary>
    /// <param name="context">The consume context.</param>
    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Payroll domain received EmployeeOnboarded event for Employee {EmployeeId}", message.EmployeeId);
        }

        // Idempotency Check: Did we already create this profile?
        var exists = _dbContext.PayrollProfiles.Any(p => p.EmployeeId == message.EmployeeId);
        if (exists)
        {
            _logger.LogWarning("Payroll profile for Employee {EmployeeId} already exists. Skipping.", message.EmployeeId);
            return;
        }

        var profile = PayrollProfile.CreateDraft(message.EmployeeId, message.LegalName);
        _dbContext.PayrollProfiles.Add(profile);

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Draft Payroll Profile created for Employee {EmployeeId}", message.EmployeeId);
        }
    }
}
