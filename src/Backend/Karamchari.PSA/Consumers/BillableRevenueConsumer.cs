namespace Karamchari.PSA.Consumers;

using EFCore.BulkExtensions;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Catches <see cref="TimesheetApprovedIntegrationEvent"/> and writes billable entries
/// into the <see cref="UnbilledRevenue"/> ledger.
///
/// This is the "Gather" side of the dual-ledger pattern:
///   Payroll consumer → salary ledger
///   THIS consumer   → revenue ledger
///
/// Both consumers are fed from the exact same event — no cross-context reads.
///
/// Retroactive corrections: when <see cref="TimesheetApprovedIntegrationEvent.IsRetroactive"/>
/// is true, existing revenue rows for the timesheet are deleted before new rows are inserted,
/// ensuring the revenue ledger reflects the latest approved figures.
/// </summary>
public sealed partial class BillableRevenueConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
{
    private readonly PSADbContext _db;
    private readonly ProjectResourceRepository _resourceRepo;
    private readonly ILogger<BillableRevenueConsumer> _logger;

    public BillableRevenueConsumer(
        PSADbContext db,
        ProjectResourceRepository resourceRepo,
        ILogger<BillableRevenueConsumer> logger)
    {
        _db = db;
        _resourceRepo = resourceRepo;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        var existingRows = await _db.UnbilledRevenue
            .Where(r => r.TimesheetId == message.TimesheetId)
            .ToListAsync(context.CancellationToken);

        if (existingRows.Count > 0)
        {
            if (!message.IsRetroactive)
            {
                // Already recorded and not a correction — idempotent skip.
                LogRevenueAlreadyRecorded(_logger, message.TimesheetId);
                return;
            }

            // Retroactive correction: reverse previous revenue entries.
            _db.UnbilledRevenue.RemoveRange(existingRows);
            LogRetroactiveCorrection(_logger, message.TimesheetId, existingRows.Count);
        }

        var billableEntries = message.Entries
            .Where(e => e.IsBillable && e.ProjectId.HasValue)
            .ToList();

        if (billableEntries.Count == 0)
        {
            LogNoBillableEntries(_logger, message.TimesheetId);

            if (existingRows.Count > 0)
                await _db.SaveChangesAsync(context.CancellationToken); // persist deletion

            return;
        }

        var revenueRows = new List<UnbilledRevenue>();

        foreach (var entry in billableEntries)
        {
            // Time-bound rate lookup: use the rate active on the WORK DATE, not today.
            // This is critical for invoicing accuracy — a rate change mid-project must
            // only affect future work, not retroactively alter historical revenue.
            var assignment = await _resourceRepo.GetActiveAssignmentAsync(
                message.EmployeeId,
                entry.ProjectId!.Value,
                entry.Date,
                context.CancellationToken);

            if (assignment is null)
            {
                // Log and skip rather than fail the whole batch — a single missing assignment
                // should not block other valid entries from being recorded.
                _logger.LogWarning(
                    "No active assignment for Employee {EmployeeId} on Project {ProjectId} on {Date}. " +
                    "Skipping this entry.",
                    message.EmployeeId, entry.ProjectId, entry.Date);
                continue;
            }

            revenueRows.Add(UnbilledRevenue.Record(
                message.TimesheetId,
                message.EmployeeId,
                entry.ProjectId!.Value,
                entry.Hours,
                assignment.BillableRate,
                assignment.Currency,
                entry.Date));
        }

        if (revenueRows.Count > 0)
        {
            await _db.BulkInsertAsync(revenueRows, cancellationToken: context.CancellationToken);
            LogRevenueRecorded(_logger, revenueRows.Count, message.TimesheetId, message.EmployeeId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Revenue already recorded for Timesheet {TimesheetId}. Skipping.")]
    private static partial void LogRevenueAlreadyRecorded(ILogger logger, Guid timesheetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retroactive correction for Timesheet {TimesheetId}: removed {Count} prior revenue rows.")]
    private static partial void LogRetroactiveCorrection(ILogger logger, Guid timesheetId, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Timesheet {TimesheetId} has no billable entries. No revenue recorded.")]
    private static partial void LogNoBillableEntries(ILogger logger, Guid timesheetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recorded {Count} billable revenue entries for Timesheet {TimesheetId}, Employee {EmployeeId}.")]
    private static partial void LogRevenueRecorded(ILogger logger, int count, Guid timesheetId, Guid employeeId);
}
