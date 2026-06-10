// -----------------------------------------------------------------------
// <copyright file="TimesheetApprovedConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.TimeAttendance.Domain.Timesheets.Events;
using MassTransit;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Translates the internal <see cref="TimesheetApproved"/> domain event into the
/// public <see cref="TimesheetApprovedIntegrationEvent"/> that crosses bounded-context
/// boundaries to Payroll, Revenue Ledger, and Utilization consumers.
///
/// Sits between the aggregate and the outside world so that:
/// - The aggregate raises a lightweight domain event (captured by the MassTransit outbox).
/// - Cross-context contracts remain independently stable from internal domain changes.
/// </summary>
public sealed class TimesheetApprovedConsumer : IConsumer<TimesheetApproved>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TimesheetApproved> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        TimesheetApproved msg = context.Message;

        var integrationEvent = new TimesheetApprovedIntegrationEvent(
            TimesheetId: msg.TimesheetId,
            EmployeeId: msg.EmployeeId,
            TenantId: msg.TenantId,
            WeekStartDate: msg.WeekStartDate,
            TotalHours: msg.Entries.Sum(e => e.Hours),
            BillableHours: msg.Entries.Where(e => e.IsBillable).Sum(e => e.Hours),
            IsRetroactive: msg.IsRetroactive,
            Entries: msg.Entries
                .Select(e => new ApprovedTimeEntryDto(
                    e.EntryId,
                    e.Date,
                    e.Hours,
                    e.ProjectId,
                    e.TaskId,
                    e.IsBillable,
                    e.CostCenter,
                    e.Tag,
                    e.Description))
                .ToList()
                .AsReadOnly());

        await context.Publish(integrationEvent).ConfigureAwait(false);
    }
}
