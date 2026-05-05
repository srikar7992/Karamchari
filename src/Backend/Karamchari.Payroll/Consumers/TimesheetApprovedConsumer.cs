namespace Karamchari.Payroll.Consumers;

using MassTransit;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Records approved timesheet hours in the localized payroll ledger.
/// </summary>
public sealed class TimesheetApprovedConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
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
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Idempotency: the TimesheetId is the natural deduplication key.
        // On retry the same event arrives with the same TimesheetId — skip silently.
        var alreadyRecorded = await _dbContext.TimesheetLedger
            .AnyAsync(t => t.TimesheetId == context.Message.TimesheetId, context.CancellationToken);

        if (alreadyRecorded) return;

        var entry = PayrollTimesheetLedger.Record(
            context.Message.TimesheetId,
            context.Message.EmployeeId,
            context.Message.WeekStartDate,
            context.Message.TotalHours);

        _dbContext.TimesheetLedger.Add(entry);

        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
