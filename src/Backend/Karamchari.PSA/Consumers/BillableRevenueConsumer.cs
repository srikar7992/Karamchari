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
/// </summary>
public sealed class BillableRevenueConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
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

        // Idempotency: if we already have revenue rows for this timesheet, skip.
        var alreadyProcessed = await _db.UnbilledRevenue
            .AnyAsync(r => r.TimesheetId == message.TimesheetId, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Revenue already recorded for Timesheet {TimesheetId}. Skipping.",
                message.TimesheetId);
            return;
        }

        // Filter to billable entries that have a project reference.
        var billableEntries = message.Entries
            .Where(e => e.IsBillable && e.ProjectId.HasValue)
            .ToList();

        if (!billableEntries.Any())
        {
            _logger.LogInformation(
                "Timesheet {TimesheetId} has no billable entries. No revenue recorded.",
                message.TimesheetId);
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

            if (assignment == null)
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
                entry.ProjectId.Value,
                entry.Hours,
                assignment.BillableRate,
                assignment.Currency,
                entry.Date));
        }

        if (revenueRows.Count > 0)
        {
            // Bulk insert for performance — avoids N individual round-trips.
            await _db.BulkInsertAsync(revenueRows, cancellationToken: context.CancellationToken);

            _logger.LogInformation(
                "Recorded {Count} billable revenue entries for Timesheet {TimesheetId}, Employee {EmployeeId}.",
                revenueRows.Count, message.TimesheetId, message.EmployeeId);
        }
    }
}
