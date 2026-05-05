namespace Karamchari.PSA.Consumers;

using EFCore.BulkExtensions;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using Karamchari.PSA.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Third consumer of <see cref="TimesheetApprovedIntegrationEvent"/>.
///
/// The event pipeline now fans out to three consumers:
///   1. PayrollConsumer → salary deductions (Payroll context)
///   2. BillableRevenueConsumer → unbilled revenue ledger (PSA context)
///   3. THIS → project profit ledger + aggregation (PSA context)
///
/// For each billable entry:
///   Revenue = Hours × BillableRate (from ProjectResource)
///   Cost    = Hours × CostPerHour  (from EmployeeCostSnapshot)
///   Profit  = Revenue − Cost
///
/// Then atomically upserts into <see cref="ProjectMonthlyMetrics"/>.
/// </summary>
public sealed class ProfitCalculationConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
{
    private readonly PSADbContext _db;
    private readonly ProjectResourceRepository _resourceRepo;
    private readonly EmployeeCostService _costService;
    private readonly ILogger<ProfitCalculationConsumer> _logger;

    public ProfitCalculationConsumer(
        PSADbContext db,
        ProjectResourceRepository resourceRepo,
        EmployeeCostService costService,
        ILogger<ProfitCalculationConsumer> logger)
    {
        _db = db;
        _resourceRepo = resourceRepo;
        _costService = costService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        // Idempotency: if profit rows already exist for this timesheet, skip.
        var alreadyProcessed = await _db.ProfitLedger
            .AnyAsync(p => p.TimesheetId == msg.TimesheetId, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Profit already calculated for Timesheet {TimesheetId}. Skipping.",
                msg.TimesheetId);
            return;
        }

        // Filter to billable entries with a project reference.
        var billableEntries = msg.Entries
            .Where(e => e.IsBillable && e.ProjectId.HasValue)
            .ToList();

        if (!billableEntries.Any())
        {
            // Still track total hours for utilization even if nothing is billable.
            await UpdateMonthlyMetricsAsync(msg, Array.Empty<ProjectProfitLedger>(), context.CancellationToken);
            return;
        }

        var profitRows = new List<ProjectProfitLedger>();

        foreach (var entry in billableEntries)
        {
            // Get the billable rate active on the work date.
            var assignment = await _resourceRepo.GetActiveAssignmentAsync(
                msg.EmployeeId,
                entry.ProjectId!.Value,
                entry.Date,
                context.CancellationToken);

            if (assignment == null)
            {
                _logger.LogWarning(
                    "No active assignment for Employee {EmployeeId} on Project {ProjectId} on {Date}. " +
                    "Skipping profit calculation for this entry.",
                    msg.EmployeeId, entry.ProjectId, entry.Date);
                continue;
            }

            // Get the employee's cost-per-hour for the month of the work date.
            var costPerHour = await _costService.GetCostPerHourAsync(
                msg.EmployeeId,
                entry.Date.Year,
                entry.Date.Month,
                context.CancellationToken) ?? 0m;

            profitRows.Add(ProjectProfitLedger.Create(
                msg.TimesheetId,
                msg.EmployeeId,
                entry.ProjectId!.Value,
                entry.Date,
                entry.Hours,
                assignment.BillableRate,
                costPerHour,
                assignment.Currency));
        }

        if (profitRows.Count > 0)
        {
            // Bulk insert raw profit entries.
            await _db.BulkInsertAsync(profitRows, cancellationToken: context.CancellationToken);

            _logger.LogInformation(
                "Recorded {Count} profit ledger entries for Timesheet {TimesheetId}.",
                profitRows.Count, msg.TimesheetId);
        }

        // Upsert monthly aggregations.
        await UpdateMonthlyMetricsAsync(msg, profitRows, context.CancellationToken);
    }

    /// <summary>
    /// Atomically upserts into the ProjectMonthlyMetrics table.
    /// Groups by (ProjectId, Year, Month) and accumulates into existing rows.
    /// </summary>
    private async Task UpdateMonthlyMetricsAsync(
        TimesheetApprovedIntegrationEvent msg,
        IReadOnlyList<ProjectProfitLedger> profitRows,
        CancellationToken ct)
    {
        // Calculate total hours from ALL entries (billable + non-billable) for utilization.
        var totalHours = msg.Entries.Sum(e => e.Hours);

        // Group profit rows by (ProjectId, Year, Month) for aggregation.
        var groups = profitRows
            .GroupBy(r => new { r.ProjectId, Year = r.WorkDate.Year, Month = r.WorkDate.Month })
            .ToList();

        foreach (var group in groups)
        {
            var existing = await _db.MonthlyMetrics
                .FirstOrDefaultAsync(m =>
                    m.ProjectId == group.Key.ProjectId &&
                    m.Year == group.Key.Year &&
                    m.Month == group.Key.Month, ct);

            decimal groupRevenue = group.Sum(r => r.Revenue);
            decimal groupCost = group.Sum(r => r.Cost);
            decimal groupBillableHours = group.Sum(r => r.Hours);

            if (existing != null)
            {
                // Accumulate into existing aggregation.
                existing.Accumulate(groupRevenue, groupCost, groupBillableHours, totalHours);
            }
            else
            {
                // Create new aggregation row.
                var metrics = ProjectMonthlyMetrics.Create(
                    group.Key.ProjectId,
                    group.Key.Year,
                    group.Key.Month,
                    groupRevenue,
                    groupCost,
                    groupBillableHours,
                    totalHours);

                _db.MonthlyMetrics.Add(metrics);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
