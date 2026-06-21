// -----------------------------------------------------------------------
// <copyright file="TimesheetService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
public sealed class TimesheetService(TimeAttendanceDbContext dbContext, ICapacityProvider capacityProvider)
{
    private readonly TimeAttendanceDbContext _dbContext = dbContext;
    private readonly ICapacityProvider _capacityProvider = capacityProvider;

    // â”€â”€ Create â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Creates a blank timesheet for a week. Idempotent â€” throws if one already exists.</summary>
    public async Task<Timesheet> CreateAsync(
        Guid employeeId,
        DateOnly weekStartDate,
        string employeeTimeZoneId = "UTC",
        CancellationToken cancellationToken = default)
    {
        bool exists = await _dbContext.Timesheets.AnyAsync(
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

    // â”€â”€ Entry management â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Replaces the full entry set.
    ///
    /// Pipeline applied before the aggregate sees the entries:
    /// 1. Cross-midnight split   â€” entries spanning midnight become two entries.
    /// 2. Capacity validation    â€” total billable hours checked against ICapacityProvider.
    /// 3. Aggregate validation   â€” per-entry invariants + overlap detection.
    /// </summary>
    public async Task UpdateEntriesAsync(
        Guid timesheetId,
        IEnumerable<TimeEntry> rawEntries,
        CancellationToken cancellationToken = default)
    {
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);

        // 1. Normalise cross-midnight spans into per-day segments.
        var normalized = rawEntries
            .SelectMany(TimesheetValidator.NormalizeAcrossMidnight)
            .ToList();

        // 2. Capacity check â€” null means no cap configured.
        decimal? capacity = await _capacityProvider.GetBillableCapacityAsync(
            timesheet.EmployeeId, timesheet.WeekStartDate, cancellationToken);
        TimesheetValidator.ValidateCapacity(normalized, capacity);

        // 3. Aggregate validates per-entry rules + overlaps.
        timesheet.UpdateEntries(normalized);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // â”€â”€ Lifecycle transitions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task SubmitAsync(Guid timesheetId, Guid actorId, CancellationToken cancellationToken = default)
    {
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Submit(actorId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Full approval â€” approves all entries and the timesheet in one operation.
    /// Triggers <c>TimesheetApproved</c> domain event â†’ outbox â†’ <c>TimesheetApprovedConsumer</c>
    /// â†’ <c>TimesheetApprovedIntegrationEventV1</c> published to Payroll, Revenue, Utilization.
    /// </summary>
    public async Task ApproveAsync(Guid timesheetId, Guid approverId, CancellationToken cancellationToken = default)
    {
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
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
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.ApproveEntry(entryId, approverId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task RejectAsync(
        Guid timesheetId,
        string reason,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
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
        Timesheet timesheet = await RequireTimesheetAsync(timesheetId, cancellationToken);
        timesheet.Reopen(reason, adminId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    // â”€â”€ Queries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<Timesheet?> FindAsync(Guid timesheetId, CancellationToken cancellationToken = default) =>
        await _dbContext.Timesheets.FindAsync([timesheetId], cancellationToken);

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IAsyncEnumerable<Timesheet> GetForEmployeeAsync(Guid employeeId, DateOnly from, DateOnly to) =>
        _dbContext.Timesheets
            .Where(t => t.EmployeeId == employeeId && t.WeekStartDate >= from && t.WeekStartDate <= to)
            .OrderBy(t => t.WeekStartDate)
            .AsAsyncEnumerable();

    // â”€â”€ Private â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task<Timesheet> RequireTimesheetAsync(Guid timesheetId, CancellationToken cancellationToken)
    {
        Timesheet? timesheet = await _dbContext.Timesheets.FindAsync([timesheetId], cancellationToken);
        return timesheet ?? throw new InvalidOperationException($"Timesheet {timesheetId} not found.");
    }
}
