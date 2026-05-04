namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts;
using Karamchari.Payroll.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Consumes the payroll lock event to trigger mass payslip generation.
/// </summary>
public sealed class PayrollRunLockedConsumer : IConsumer<PayrollRunLockedIntegrationEvent>
{
    private readonly PayrollDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="PayrollRunLockedConsumer"/> class.
    /// </summary>
    public PayrollRunLockedConsumer(PayrollDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PayrollRunLockedIntegrationEvent> context)
    {
        var message = context.Message;

        // 1. Fetch all ledger entries for this run
        var entries = await _dbContext.PayrollLedger
            .Where(e => e.RunId == message.RunId)
            .ToListAsync(context.CancellationToken);

        // 2. Publish individual completion events to trigger QuestPDF
        foreach (var entry in entries)
        {
            // We need the tax regime, which is stored in the profile
            var profile = await _dbContext.PayrollProfiles
                .FirstOrDefaultAsync(p => p.EmployeeId == entry.EmployeeId, context.CancellationToken);

            await _publishEndpoint.Publish(new PayrollRunCompletedIntegrationEvent(
                entry.EmployeeId,
                message.TenantId,
                message.PeriodName,
                entry.MonthlyGross,
                entry.NetPay,
                entry.Earnings.ToDictionary(k => k.Key, v => v.Value),
                entry.Deductions.ToDictionary(k => k.Key, v => v.Value),
                profile?.TaxRegime.ToString() ?? "New"
            ), context.CancellationToken);
        }
    }
}
