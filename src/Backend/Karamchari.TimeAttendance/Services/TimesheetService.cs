using Karamchari.TimeAttendance.Domain.Timesheets;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Application service for the Timesheet aggregate.
///
/// Responsibility boundary:
/// - Loads/saves the aggregate via EF Core.
/// - Applies cross-midnight normalisation and capacity validation before
///   delegating to the aggregate's own command methods.
/// - Does NOT touch IPublishEndpoint directly. Domain events raised by the
///   aggregate are drained by DomainEventDispatchInterceptor inside SaveChangesAsync
///   and captured by MassTransit's transactional outbox atomically.
/// </summary>
public sealed class TimesheetService
{
    private readonly TimeAttendanceDbContext _dbContext;
    private readonly ICapacityProvider _capacityProvider;

    public TimesheetService(TimeAttendanceDbContext dbContext, ICapacityProvider capacityProvider)
    {
        _dbContext = dbContext;
        _capacityProvider = capacityProvider;
    }

    // ── Create ───────────────────────────────────────────────────────────────

    /// <summary>Creates a blank timesheet for a week. Idempotent — throws if one already exists.</summary>
    public async Task<Timesheet> CreateAsync(
        Guid employeeId,
        DateOnly weekStartDate,
        string employeeTimeZoneId = "UTC",
        CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Timesheets.AnyAsync(
            t => t.EmployeeId == employeeId && t.WeekStartDate == weekStartDate,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                $"A timesheet already exists for employee {employeeId} for week starting {weekStartDate:d}.");

        var timesheet = Timesheet.Create(employeeId, weekStartDate, employeeTimeZoneId);
        _dbContext.Timesheets.Add(timesheet);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return timesheet;
    }

    // ── Entry management ─────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the full entry set.
    ///
    /// Pipeline applied before the aggregate sees the entries:
    /// 1. Cross-midnight split   — entries spanning midnight become two entries.
    /// 2. Capacity validation    — total billable hours checked against ICapacityProvider.
    /// 3. Aggregate validation   — per-entry invariants + overlap detection.
    /// </summary>
    public async Task UpdateEntriesAsync(
        Guid timesheetId,
        IEnumerable<TimeEntry> rawEntries,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);

        // 1. Normalise cross-midnight spans into per-day segments.
        var normalized = rawEntries
            .SelectMany(TimesheetValidator.NormalizeAcrossMidnight)
            .ToList();

        // 2. Capacity check — null means no cap configured.
        var capacity = await _capacityProvider.GetBillableCapacityAsync(
            timesheet.EmployeeId, timesheet.WeekStartDate, cancellationToken);
        TimesheetValidator.ValidateCapacity(normalized, capacity);

        // 3. Aggregate validates per-entry rules + overlaps.
        timesheet.UpdateEntries(normalized);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Lifecycle transitions ─────────────────────────────────────────────────

    public async Task SubmitAsync(Guid timesheetId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Submit(actorId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Full approval — approves all entries and the timesheet in one operation.
    /// Triggers <c>TimesheetApproved</c> domain event → outbox → <c>TimesheetApprovedConsumer</c>
    /// → <c>TimesheetApprovedIntegrationEvent</c> published to Payroll, Revenue, Utilization.
    /// </summary>
    public async Task ApproveAsync(Guid timesheetId, Guid approverId, CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Approve(approverId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Row-level approval. Auto-approves the whole timesheet when the last entry is approved.
    /// </summary>
    public async Task ApproveEntryAsync(
        Guid timesheetId,
        Guid entryId,
        Guid approverId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.ApproveEntry(entryId, approverId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        Guid timesheetId,
        string reason,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Reject(reason, actorId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Reopens an approved timesheet for retroactive editing.
    /// Triggers <c>TimesheetReopened</c> domain event so downstream systems can
    /// prepare for an incoming correction.
    /// </summary>
    public async Task ReopenAsync(
        Guid timesheetId,
        string reason,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Reopen(reason, adminId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<Timesheet?> FindAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        await _dbContext.Timesheets.FindAsync([timesheetId], cancellationToken);

    public IAsyncEnumerable<Timesheet> GetForEmployeeAsync(Guid employeeId, DateOnly from, DateOnly to) =>
        _dbContext.Timesheets
            .Where(t => t.EmployeeId == employeeId && t.WeekStartDate >= from && t.WeekStartDate <= to)
            .OrderBy(t => t.WeekStartDate)
            .AsAsyncEnumerable();

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task<Timesheet> RequireTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken)
    {
        var timesheet = await _dbContext.Timesheets.FindAsync([timesheetId], cancellationToken);
        return timesheet ?? throw new InvalidOperationException($"Timesheet {timesheetId} not found.");
    }
}
