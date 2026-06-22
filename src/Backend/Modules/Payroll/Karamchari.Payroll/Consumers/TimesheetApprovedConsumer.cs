// -----------------------------------------------------------------------
// <copyright file="TimesheetApprovedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Records approved timesheet hours in the localised payroll ledger.
/// Handles retroactive corrections: when <see cref="TimesheetApprovedIntegrationEventV1.IsRetroactive"/>
/// is true the existing ledger entry is deleted before the corrected one is inserted,
/// ensuring the ledger always reflects the latest approved figures.
/// </summary>
public sealed class TimesheetApprovedConsumer : IConsumer<TimesheetApprovedIntegrationEventV1>
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimesheetApprovedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The payroll database context.</param>
    public TimesheetApprovedConsumer(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Consumes the timesheet approved event and records it in the ledger.
    /// </summary>
    /// <param name="context">The consumer context.</param>
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEventV1> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        var existing = await _dbContext.TimesheetLedger
            .FirstOrDefaultAsync(t => t.TimesheetId == msg.TimesheetId, context.CancellationToken);

        if (existing != null)
        {
            if (!msg.IsRetroactive)
            {
                // Already recorded and not a correction â€” skip (idempotency).
                return;
            }

            // Retroactive correction: remove the previous entry so it can be replaced.
            _dbContext.TimesheetLedger.Remove(existing);
        }

        var entry = PayrollTimesheetLedger.Record(
            msg.TimesheetId,
            msg.EmployeeId,
            msg.WeekStartDate,
            msg.TotalHours);

        _dbContext.TimesheetLedger.Add(entry);

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
