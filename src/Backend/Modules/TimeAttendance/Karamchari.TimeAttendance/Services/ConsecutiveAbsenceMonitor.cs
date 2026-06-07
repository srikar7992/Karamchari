using Karamchari.TimeAttendance.Contracts;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Policy;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Detects N consecutive absent days (excluding approved leave) and triggers HR escalation.
/// Run as part of the nightly attendance finalization job.
/// </summary>
public sealed class ConsecutiveAbsenceMonitor(
    TimeAttendanceDbContext db,
    AttendancePolicyHierarchyResolver policyResolver,
    IPublishEndpoint bus,
    ILogger<ConsecutiveAbsenceMonitor> logger)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly AttendancePolicyHierarchyResolver _policyResolver = policyResolver;
    private readonly IPublishEndpoint _bus = bus;
    private readonly ILogger<ConsecutiveAbsenceMonitor> _logger = logger;

    public sealed record ConsecutiveAbsenceAlert(
        Guid EmployeeId,
        int ConsecutiveDays,
        DateOnly FirstAbsentDate,
        DateOnly LastAbsentDate);

    /// <summary>
    /// Checks all employees in the tenant for consecutive absences.
    /// Returns alerts for employees who have exceeded the policy threshold.
    /// Callers (notification service, HR dashboard) act on the returned alerts.
    /// </summary>
    public async Task<IReadOnlyList<ConsecutiveAbsenceAlert>> DetectAsync(
        string tenantId,
        DateOnly asOfDate,
        CancellationToken ct = default)
    {
        AttendancePolicyHierarchy policy = await _policyResolver.ResolveForEmployeeAsync(tenantId, Guid.Empty, ct: ct);
        int threshold = policy.ConsecutiveAbsenceEscalationDays;

        // Look back threshold+1 days to find streaks
        DateOnly lookbackDate = asOfDate.AddDays(-(threshold + 1));

        var recentRecords = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= lookbackDate &&
                r.WorkDate <= asOfDate &&
                (r.Status == AttendanceStatus.Absent))
            .OrderBy(r => r.EmployeeId)
            .ThenBy(r => r.WorkDate)
            .Select(r => new { r.EmployeeId, r.WorkDate })
            .ToListAsync(ct);

        // Leave-covered absences should not count — exclude OnLeave records
        var leaveRecords = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= lookbackDate &&
                r.WorkDate <= asOfDate &&
                r.Status == AttendanceStatus.OnLeave)
            .Select(r => new { r.EmployeeId, r.WorkDate })
            .ToListAsync(ct);

        var leaveSet = leaveRecords
            .Select(r => (r.EmployeeId, r.WorkDate))
            .ToHashSet();

        var alerts = new List<ConsecutiveAbsenceAlert>();

        var byEmployee = recentRecords
            .Where(r => !leaveSet.Contains((r.EmployeeId, r.WorkDate)))
            .GroupBy(r => r.EmployeeId);

        foreach (var group in byEmployee)
        {
            var dates = group.Select(r => r.WorkDate).OrderBy(d => d).ToList();
            int streak = 1;
            DateOnly streakStart = dates[0];

            for (int i = 1; i < dates.Count; i++)
            {
                // Count only consecutive calendar days (ignore weekends/holidays here — they should be
                // filtered upstream by having WeekOff/Holiday status rather than Absent)
                if (dates[i] == dates[i - 1].AddDays(1))
                {
                    streak++;
                    if (streak >= threshold)
                    {
                        alerts.Add(new ConsecutiveAbsenceAlert(
                            group.Key, streak, streakStart, dates[i]));
                        break; // One alert per employee per detection run
                    }
                }
                else
                {
                    streak = 1;
                    streakStart = dates[i];
                }
            }
        }

        if (alerts.Count > 0)
        {
            _logger.LogInformation(
                "ConsecutiveAbsenceMonitor: {AlertCount} employees exceeded {Threshold}-day threshold on {Date}.",
                alerts.Count, threshold, asOfDate);

            foreach (ConsecutiveAbsenceAlert alert in alerts)
            {
                await _bus.Publish(new ConsecutiveAbsenceEscalatedIntegrationEvent(
                    Guid.NewGuid(),
                    tenantId,
                    alert.EmployeeId,
                    alert.ConsecutiveDays,
                    alert.FirstAbsentDate,
                    alert.LastAbsentDate,
                    DateTimeOffset.UtcNow), ct);
            }
        }

        return alerts;
    }
}
