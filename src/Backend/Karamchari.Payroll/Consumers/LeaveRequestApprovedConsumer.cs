namespace Karamchari.Payroll.Consumers;

using MassTransit;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consumes the LeaveRequestApprovedIntegrationEvent and records a deduction in the Payroll ledger if the leave is unpaid.
/// </summary>
public sealed class LeaveRequestApprovedConsumer : IConsumer<LeaveRequestApprovedIntegrationEvent>
{
    private readonly PayrollDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaveRequestApprovedConsumer"/> class.
    /// </summary>
    /// <param name="dbContext">The payroll database context.</param>
    public LeaveRequestApprovedConsumer(PayrollDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Consumes the event and calculates the monetary deduction.
    /// </summary>
    /// <param name="context">The consumer context.</param>
    public async Task Consume(ConsumeContext<LeaveRequestApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Only process unpaid leave for monetary deduction
        if (context.Message.IsPaid)
        {
            return;
        }

        var profile = await _dbContext.PayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == context.Message.EmployeeId);

        if (profile == null)
        {
            // If the employee has no payroll profile, we cannot deduct pay.
            return;
        }

        // Daily rate calculation (standard 22-day working month assumption)
        decimal dailyRate = profile.BaseSalary / 22m;
        decimal deductionAmount = Math.Round(dailyRate * (decimal)context.Message.TotalDays, 2);

        // Determine the target payroll period based on the leave start date
        string periodName = context.Message.StartDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

        var deduction = PayrollDeduction.Create(
            context.Message.EmployeeId,
            periodName,
            deductionAmount,
            $"Unpaid Leave: {context.Message.TotalDays} days ({context.Message.StartDate:MM/dd} - {context.Message.EndDate:MM/dd})");

        _dbContext.PayrollDeductions.Add(deduction);
        await _dbContext.SaveChangesAsync();
    }
}
