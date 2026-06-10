// -----------------------------------------------------------------------
// <copyright file="PayrollRunLockedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Consumers;

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V2;
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
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        // 1. Fetch all ledger entries for this run.
        var entries = await _dbContext.PayrollLedger
            .Where(e => e.RunId == message.RunId)
            .ToListAsync(context.CancellationToken);

        if (entries.Count == 0) return;

        // 2. Bulk-load all profiles in one query â€” eliminates the N+1 that previously
        //    issued one SELECT per ledger entry inside the loop.
        var employeeIds = entries.Select(e => e.EmployeeId).ToHashSet();
        var profilesById = await _dbContext.PayrollProfiles
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .ToDictionaryAsync(p => p.EmployeeId, context.CancellationToken);

        // 3. Publish individual completion events to trigger payslip generation.
        //    Inside a MassTransit consumer, IPublishEndpoint is wired to the transactional
        //    outbox when the backing DbContext has AddEntityFrameworkOutbox configured.
        //    The SaveChangesAsync call below writes the outbox rows atomically â€” events are
        //    only delivered if the commit succeeds.
        foreach (var entry in entries)
        {
            profilesById.TryGetValue(entry.EmployeeId, out var profile);

            await _publishEndpoint.Publish(new PayrollRunCompletedIntegrationEvent(
                entry.EmployeeId,
                profile?.EmployeeName ?? "Unknown Employee",
                message.TenantId,
                message.PeriodName,
                entry.MonthlyGross,
                entry.NetPay,
                entry.Earnings.ToDictionary(k => k.Key, v => v.Value),
                entry.Deductions.ToDictionary(k => k.Key, v => v.Value),
                profile?.TaxRegime.ToString() ?? "New"
            ), context.CancellationToken);
        }

        // Flush outbox rows atomically. Without this, the Publish calls above are buffered
        // but never committed and the payslip generation events are silently dropped.
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
