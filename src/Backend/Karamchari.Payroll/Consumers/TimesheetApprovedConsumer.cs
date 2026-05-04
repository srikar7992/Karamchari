namespace Karamchari.Payroll.Consumers;

using MassTransit;
using Karamchari.TimeAttendance.Contracts;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;

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

        var entry = PayrollTimesheetLedger.Record(
            context.Message.TimesheetId,
            context.Message.EmployeeId,
            context.Message.WeekStartDate,
            context.Message.TotalHours);

        _dbContext.TimesheetLedger.Add(entry);
        
        await _dbContext.SaveChangesAsync();
    }
}
