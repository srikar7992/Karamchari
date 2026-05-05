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

        // Only process unpaid leave for monetary deduction.
        if (context.Message.IsPaid) return;

        // Idempotency: use RequestId as the deduplication key. The Reason column stores it
        // so retries produce no duplicate rows. A more robust approach is a dedicated
        // SourceEventId column with a unique index; this is a low-cost interim guard.
        var idempotencyMarker = $"LeaveRequest:{context.Message.RequestId}";
        var alreadyRecorded = await _dbContext.PayrollDeductions
            .AnyAsync(d => d.Reason == idempotencyMarker, context.CancellationToken);

        if (alreadyRecorded) return;

        var profile = await _dbContext.PayrollProfiles
            .FirstOrDefaultAsync(p => p.EmployeeId == context.Message.EmployeeId, context.CancellationToken);

        if (profile == null)
        {
            // Employee has no active payroll profile — cannot deduct pay.
            return;
        }

        // Daily rate: derive from the monthly gross implied by AnnualCTC (22 working days/month).
        // Using AnnualCTC / 12 / 22 is more accurate than BaseSalary for salaried employees.
        decimal monthlyGross = profile.AnnualCTC / 12m;
        decimal dailyRate = monthlyGross / 22m;
        decimal deductionAmount = Math.Round(dailyRate * (decimal)context.Message.TotalDays, 2);

        // Determine the target payroll period based on the leave start date.
        string periodName = context.Message.StartDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

        var deduction = PayrollDeduction.Create(
            context.Message.EmployeeId,
            periodName,
            deductionAmount,
            idempotencyMarker);

        _dbContext.PayrollDeductions.Add(deduction);
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
