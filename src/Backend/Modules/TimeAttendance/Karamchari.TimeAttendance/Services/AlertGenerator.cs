// -----------------------------------------------------------------------
// <copyright file="AlertGenerator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Manager alert queue — surfaces items needing human attention right now.
/// Combines open Critical/High violations with SLA-breached regularization requests.
/// </summary>
public sealed class AlertGenerator(TimeAttendanceDbContext db)
{
    private readonly TimeAttendanceDbContext _db = db;

    public async Task<IReadOnlyList<ManagerAlertRecord>> GetManagerAlertsAsync(
        string tenantId,
        int maxAlerts = 50,
        CancellationToken ct = default)
    {
        var alerts = new List<ManagerAlertRecord>();

        DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-7);
        List<AttendanceViolation> openViolations = await _db.AttendanceViolations
            .Where(v =>
                v.TenantId == tenantId &&
                v.Status == AttendanceExceptionStatus.Open &&
                (v.Severity == AttendanceExceptionSeverity.Critical || v.Severity == AttendanceExceptionSeverity.High) &&
                v.CreatedAt >= since)
            .OrderByDescending(v => v.CreatedAt)
            .Take(maxAlerts)
            .ToListAsync(ct);

        foreach (AttendanceViolation? v in openViolations)
        {
            alerts.Add(new ManagerAlertRecord(
                v.EmployeeId,
                v.ExceptionType.ToString(),
                v.Description,
                v.Severity,
                v.CreatedAt));
        }

        // Regularizations pending > 24h = SLA breach risk
        DateTimeOffset regularizationCutoff = DateTimeOffset.UtcNow.AddHours(-24);
        List<RegularizationRequest> stalePending = await _db.RegularizationRequests
            .Where(r =>
                r.TenantId == tenantId &&
                r.Status == RegularizationStatus.Submitted &&
                r.RequestedAt < regularizationCutoff)
            .Take(maxAlerts)
            .ToListAsync(ct);

        foreach (RegularizationRequest? r in stalePending)
        {
            alerts.Add(new ManagerAlertRecord(
                r.EmployeeId,
                "RegularizationPending",
                $"Regularization pending for >24h. Submitted: {r.RequestedAt:yyyy-MM-dd HH:mm}.",
                AttendanceExceptionSeverity.Medium,
                r.RequestedAt));
        }

        return alerts
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.TriggeredAt)
            .Take(maxAlerts)
            .ToList()
            .AsReadOnly();
    }
}
