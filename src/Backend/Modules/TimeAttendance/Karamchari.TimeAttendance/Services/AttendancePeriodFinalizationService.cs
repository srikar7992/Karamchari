// -----------------------------------------------------------------------
// <copyright file="AttendancePeriodFinalizationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Contracts;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.TimeAttendance.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Closes an attendance period and emits <see cref="AttendancePeriodFinalizedIntegrationEvent"/>
/// for the Payroll module. Idempotent — safe to re-run for same period (skips already-finalized records).
///
/// Call from a scheduled job at period-end (e.g., last calendar day of month + 1).
/// </summary>
public sealed class AttendancePeriodFinalizationService(
    TimeAttendanceDbContext db,
    AttendanceProcessingEngine processingEngine,
    IPublishEndpoint bus,
    ILogger<AttendancePeriodFinalizationService> logger)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly AttendanceProcessingEngine _processingEngine = processingEngine;
    private readonly IPublishEndpoint _bus = bus;
    private readonly ILogger<AttendancePeriodFinalizationService> _logger = logger;

    /// <summary>
    /// Finalizes all attendance records for the given year/month and publishes the payroll integration event.
    /// </summary>
    public async Task<FinalizeResult> FinalizeAsync(
        string tenantId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var periodStart = new DateOnly(year, month, 1);
        DateOnly periodEnd = periodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation(
            "Starting period finalization. Tenant={TenantId} Period={Year}-{Month:D2}",
            tenantId, year, month);

        // First, run EOD finalization for any still-open records (no-shows / missing checkouts)
        for (DateOnly d = periodStart; d <= periodEnd; d = d.AddDays(1))
        {
            await _processingEngine.FinalizeShiftsForDateAsync(tenantId, d, gracePeriodMinutes: 0, ct);
        }

        // Load all records for the period
        List<AttendanceRecord> records = await _db.AttendanceRecords
            .Where(r =>
                r.TenantId == tenantId &&
                r.WorkDate >= periodStart &&
                r.WorkDate <= periodEnd &&
                r.Status != AttendanceStatus.Pending)
            .ToListAsync(ct);

        // Load comp-off grants accrued this period
        List<CompOffGrant> compOffGrants = await _db.CompOffGrants
            .Where(g =>
                g.TenantId == tenantId &&
                g.WorkedOnDate >= periodStart &&
                g.WorkedOnDate <= periodEnd &&
                g.Status == CompOffGrantStatus.Active)
            .ToListAsync(ct);

        var compOffByEmployee = compOffGrants
            .GroupBy(g => g.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.DaysEarned));

        // Build per-employee summaries
        IEnumerable<IGrouping<Guid, AttendanceRecord>> byEmployee = records.GroupBy(r => r.EmployeeId);
        var summaries = new List<AttendanceEmployeeSummary>();

        int totalFinalized = 0;
        foreach (IGrouping<Guid, AttendanceRecord> group in byEmployee)
        {
            Guid employeeId = group.Key;
            var empRecords = group.ToList();

            int presentDays = empRecords.Count(r => r.Status == AttendanceStatus.Present);
            int lateDays = empRecords.Count(r => r.Status == AttendanceStatus.Late);
            int absentDays = empRecords.Count(r => r.Status == AttendanceStatus.Absent);
            int halfDays = empRecords.Count(r => r.Status == AttendanceStatus.HalfDay);
            int leaveDays = empRecords.Count(r => r.Status == AttendanceStatus.OnLeave);
            int holidayWorkDays = empRecords.Count(r => r.Status == AttendanceStatus.Present && r.OvertimeMinutes > 0);
            decimal totalWorkedHours = empRecords.Sum(r => r.WorkedHours);
            decimal overtimeHours = empRecords.Sum(r => r.OvertimeHours);
            decimal compOffAccrued = compOffByEmployee.TryGetValue(employeeId, out decimal co) ? co : 0m;

            // LOP = absent days where no leave was approved
            int lopDays = absentDays;

            summaries.Add(new AttendanceEmployeeSummary(
                employeeId,
                presentDays + lateDays, // late = present, just flagged
                absentDays,
                lateDays,
                halfDays,
                leaveDays,
                holidayWorkDays,
                totalWorkedHours,
                overtimeHours,
                compOffAccrued,
                lopDays));

            // Mark each record as finalized-for-payroll (idempotent)
            foreach (AttendanceRecord? rec in empRecords.Where(r => r.FinalizedAtUtc is null))
            {
                rec.FinalizeForPayroll();
                totalFinalized++;
            }
        }

        await _db.SaveChangesAsync(ct);

        var evt = new AttendancePeriodFinalizedIntegrationEvent(
            Guid.NewGuid(),
            tenantId,
            year,
            month,
            DateTimeOffset.UtcNow,
            summaries);

        await _bus.Publish(evt, ct);

        _logger.LogInformation(
            "Period finalization complete. Tenant={TenantId} Period={Year}-{Month:D2} Employees={EmployeeCount} RecordsFinalized={RecordCount}",
            tenantId, year, month, summaries.Count, totalFinalized);

        return new FinalizeResult(summaries.Count, totalFinalized, evt.CorrelationId);
    }

    public sealed record FinalizeResult(int EmployeeCount, int RecordsFinalized, Guid CorrelationId);
}
