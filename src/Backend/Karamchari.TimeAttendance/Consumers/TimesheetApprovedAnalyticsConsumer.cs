using Karamchari.TimeAttendance.Contracts;
using Karamchari.TimeAttendance.Domain.Analytics;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Consumers;

/// <summary>
/// Consumes <see cref="TimesheetApprovedIntegrationEvent"/> and upserts
/// pre-aggregated <see cref="ProjectMetrics"/> and <see cref="EmployeeMetrics"/>.
///
/// Idempotency: deduplicates on <see cref="KaramchariIntegrationEvent.EventId"/> via ProcessedEventLog.
/// Replay-safety: IsRetroactive → scoped recompute for (ProjectId|EmployeeId, AffectedDates).
/// Concurrency: UPDATE-then-INSERT pattern instead of MERGE (avoids SQL Server MERGE edge cases).
/// </summary>
public sealed class TimesheetApprovedAnalyticsConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
{
    private const string ConsumerName = nameof(TimesheetApprovedAnalyticsConsumer);

    private readonly TimeAttendanceDbContext _db;

    public TimesheetApprovedAnalyticsConsumer(TimeAttendanceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        var ev = context.Message;

        // 1. Event quality gate
        if (ev.ActorId == Guid.Empty)
            throw new InvalidOperationException(
                $"TimesheetApproved {ev.TimesheetId} missing ActorId. Dead-lettering.");

        // 2. Idempotency check — exact-once via ProcessedEventLog
        var alreadyProcessed = await _db.ProcessedEventLogs
            .AnyAsync(l => l.EventId == ev.EventId && l.ConsumerName == ConsumerName,
                context.CancellationToken);

        if (alreadyProcessed)
            return;

        // 3. Project metrics
        foreach (var entry in ev.Entries.Where(e => e.ProjectId.HasValue))
        {
            if (ev.IsRetroactive)
                await RecomputeProjectMetricsAsync(ev.TenantId, entry.ProjectId!.Value, entry.Date, context.CancellationToken);
            else
                await UpsertProjectMetricsAsync(ev.TenantId, entry, ev.EventId, ev.OccurredAt, context.CancellationToken);
        }

        // 4. Employee metrics — always scoped to affected dates only
        foreach (var (date, dateEntries) in ev.Entries
            .GroupBy(e => e.Date)
            .Select(g => (g.Key, g.AsEnumerable())))
        {
            await UpsertEmployeeMetricsAsync(
                ev.TenantId, ev.EmployeeId, date, dateEntries, ev.IsRetroactive, ev.EventId, ev.OccurredAt, context.CancellationToken);
        }

        // 5. Stamp as processed
        _db.ProcessedEventLogs.Add(new ProcessedEventLog
        {
            TenantId = ev.TenantId,
            EventId = ev.EventId,
            ConsumerName = ConsumerName,
            ProcessedAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(context.CancellationToken);
    }

    // UPDATE-then-INSERT with DB-level idempotency (WHERE LastEventId != @eventId)
    // and last-write-wins ordering (WHERE LastProcessedOccurredAt < @occurredAt OR NULL)
    private async Task UpsertProjectMetricsAsync(
        string tenantId, TimeEntryRecord entry, Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var projectId = entry.ProjectId!.Value;
        var billableHours = entry.IsBillable ? entry.Hours : 0m;

        var rows = await _db.Database.ExecuteSqlRawAsync(
            @"UPDATE Analytics_ProjectMetrics
              SET BillableHours             = BillableHours + {0},
                  TotalHours                = TotalHours    + {1},
                  LastEventId               = {2},
                  LastProcessedOccurredAt   = {3},
                  LastUpdatedAt             = {4}
              WHERE TenantId = {5} AND ProjectId = {6} AND Date = {7}
                AND LastEventId != {2}
                AND (LastProcessedOccurredAt IS NULL OR LastProcessedOccurredAt < {3})",
            billableHours, entry.Hours, eventId, occurredAt, DateTimeOffset.UtcNow,
            tenantId, projectId, entry.Date, ct);

        if (rows == 0)
        {
            // Row may not exist yet (first insert) — attempt insert, ignore if duplicate key
            try
            {
                _db.ProjectMetrics.Add(new ProjectMetrics
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    Date = entry.Date,
                    BillableHours = billableHours,
                    TotalHours = entry.Hours,
                    Revenue = 0,
                    Cost = 0,
                    LastEventId = eventId,
                    LastProcessedOccurredAt = occurredAt,
                    LastUpdatedAt = DateTimeOffset.UtcNow,
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // PK violation = row inserted by concurrent request — safe to swallow
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task RecomputeProjectMetricsAsync(
        string tenantId, Guid projectId, DateOnly date, CancellationToken ct)
    {
        // Scoped recompute: only this project + date (not entire dataset)
        var allEntries = await _db.Set<Domain.Timesheets.Timesheet>()
            .Where(t => t.TenantId == tenantId && t.Status == Domain.Timesheets.TimesheetStatus.Approved)
            .SelectMany(t => t.Entries)
            .Where(e => e.ProjectId == projectId && e.Date == date)
            .ToListAsync(ct);

        var billable = allEntries.Where(e => e.IsBillable).Sum(e => e.Hours);
        var total = allEntries.Sum(e => e.Hours);

        var rows = await _db.Database.ExecuteSqlRawAsync(
            @"UPDATE Analytics_ProjectMetrics
              SET BillableHours = {0}, TotalHours = {1}, LastUpdatedAt = {2}
              WHERE TenantId = {3} AND ProjectId = {4} AND Date = {5}",
            billable, total, DateTimeOffset.UtcNow, tenantId, projectId, date, ct);

        if (rows == 0)
        {
            _db.ProjectMetrics.Add(new ProjectMetrics
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Date = date,
                BillableHours = billable,
                TotalHours = total,
                LastUpdatedAt = DateTimeOffset.UtcNow,
            });
        }
    }

    private async Task UpsertEmployeeMetricsAsync(
        string tenantId, Guid employeeId, DateOnly date,
        IEnumerable<TimeEntryRecord> entries, bool isRetroactive,
        Guid eventId, DateTimeOffset occurredAt, CancellationToken ct)
    {
        var billable = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
        var total = entries.Sum(e => e.Hours);

        if (isRetroactive)
        {
            var allApproved = await _db.Set<Domain.Timesheets.Timesheet>()
                .Where(t => t.TenantId == tenantId && t.EmployeeId == employeeId
                    && t.Status == Domain.Timesheets.TimesheetStatus.Approved)
                .SelectMany(t => t.Entries)
                .Where(e => e.Date == date)
                .ToListAsync(ct);

            billable = allApproved.Where(e => e.IsBillable).Sum(e => e.Hours);
            total = allApproved.Sum(e => e.Hours);

            var rows = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE Analytics_EmployeeMetrics
                  SET BillableHours = {0}, TotalHours = {1},
                      LastEventId = {2}, LastProcessedOccurredAt = {3}, LastUpdatedAt = {4}
                  WHERE TenantId = {5} AND EmployeeId = {6} AND Date = {7}
                    AND (LastProcessedOccurredAt IS NULL OR LastProcessedOccurredAt < {3})",
                billable, total, eventId, occurredAt, DateTimeOffset.UtcNow,
                tenantId, employeeId, date, ct);

            if (rows > 0) return;
        }
        else
        {
            var rows = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE Analytics_EmployeeMetrics
                  SET BillableHours = BillableHours + {0},
                      TotalHours    = TotalHours    + {1},
                      LastEventId   = {2},
                      LastProcessedOccurredAt = {3},
                      LastUpdatedAt = {4}
                  WHERE TenantId = {5} AND EmployeeId = {6} AND Date = {7}
                    AND LastEventId != {2}
                    AND (LastProcessedOccurredAt IS NULL OR LastProcessedOccurredAt < {3})",
                billable, total, eventId, occurredAt, DateTimeOffset.UtcNow,
                tenantId, employeeId, date, ct);

            if (rows > 0) return;
        }

        _db.EmployeeMetrics.Add(new EmployeeMetrics
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            BillableHours = billable,
            TotalHours = total,
            LastEventId = eventId,
            LastProcessedOccurredAt = occurredAt,
            LastUpdatedAt = DateTimeOffset.UtcNow,
        });
    }
}
