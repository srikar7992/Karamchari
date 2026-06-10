// -----------------------------------------------------------------------
// <copyright file="PolicyViolationDetectionService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Evaluates incoming workforce data against policy rules and creates violations.
/// Called by event consumers and the nightly recompute job.
/// Compliance never modifies source systems — detect, record, escalate only.
/// </summary>
public sealed class PolicyViolationDetectionService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<PolicyViolationDetectionService> _logger;

    public PolicyViolationDetectionService(ComplianceDbContext db, ILogger<PolicyViolationDetectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Checks for excessive overtime: > 60 hours/week.
    /// Creates a High-severity violation if no open violation exists for this policy+employee this week.
    /// </summary>
    public async Task CheckExcessiveOvertimeAsync(
        string tenantId, Guid employeeId, decimal weeklyHours, DateOnly weekStart, CancellationToken ct = default)
    {
        const decimal overtimeThreshold = 60m;
        if (weeklyHours <= overtimeThreshold) return;

        var policyCode = "POL-OT-001";

        var weekStartDt = weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var weekEnd = weekStartDt.AddDays(7);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= weekStartDt
                     && v.DetectedAt < weekEnd)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            WeekStart = weekStart,
            WeeklyHours = weeklyHours,
            Threshold = overtimeThreshold
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.High, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Overtime violation detected: Employee {EmployeeId} worked {Hours:F1}h in week {Week}",
            employeeId, weeklyHours, weekStart);
    }

    /// <summary>
    /// Checks for underpayment: gross pay below configured minimum wage.
    /// Minimum wage provided by caller (from RegulatoryRegister lookup).
    /// </summary>
    public async Task CheckUnderpaymentAsync(
        string tenantId, Guid employeeId, decimal grossPay, decimal minimumWage, DateTime periodDate, CancellationToken ct = default)
    {
        if (grossPay >= minimumWage) return;

        var policyCode = "POL-WAGE-001";

        var monthStart = new DateTime(periodDate.Year, periodDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= monthStart
                     && v.DetectedAt < monthEnd)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            GrossPay = grossPay,
            MinimumWage = minimumWage,
            Deficit = minimumWage - grossPay,
            Period = periodDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.Critical, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Underpayment violation detected: Employee {EmployeeId} paid {Pay:C} vs minimum {Min:C}",
            employeeId, grossPay, minimumWage);
    }

    /// <summary>
    /// Checks for excessive absence rate: absent days / total working days > 20%.
    /// Policy code: POL-ABS-001, Severity: Medium.
    /// </summary>
    public async Task CheckExcessiveAbsenceAsync(
        string tenantId, Guid employeeId, int absentDays, int totalWorkingDays, DateTime periodDate, CancellationToken ct = default)
    {
        if (totalWorkingDays <= 0) return;
        var absenceRate = (decimal)absentDays / totalWorkingDays;
        const decimal absenceThreshold = 0.20m;
        if (absenceRate <= absenceThreshold) return;

        var policyCode = "POL-ABS-001";
        var monthStart = new DateTime(periodDate.Year, periodDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= monthStart
                     && v.DetectedAt < monthEnd)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            AbsentDays = absentDays,
            TotalWorkingDays = totalWorkingDays,
            AbsenceRate = absenceRate,
            Threshold = absenceThreshold,
            Period = periodDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.Medium, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Absence violation detected: Employee {EmployeeId} absence rate {Rate:P0} in {Period:MMM yyyy}",
            employeeId, absenceRate, periodDate);
    }

    /// <summary>
    /// Checks for consecutive shift violations: more than 14 consecutive working days without a day off.
    /// Policy code: POL-SHIFT-001, Severity: High.
    /// </summary>
    public async Task CheckConsecutiveShiftsAsync(
        string tenantId, Guid employeeId, int consecutiveDays, DateTime referenceDate, CancellationToken ct = default)
    {
        const int consecutiveThreshold = 14;
        if (consecutiveDays <= consecutiveThreshold) return;

        var policyCode = "POL-SHIFT-001";
        var weekStart = referenceDate.AddDays(-7);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= weekStart
                     && v.DetectedAt <= referenceDate)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            ConsecutiveDays = consecutiveDays,
            Threshold = consecutiveThreshold,
            ReferenceDate = referenceDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.High, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Consecutive shift violation detected: Employee {EmployeeId} worked {Days} consecutive days",
            employeeId, consecutiveDays);
    }

    /// <summary>
    /// Checks for missing break: shift > 6 hours with less than 20 minutes break taken.
    /// Policy code: POL-BREAK-001, Severity: High.
    /// </summary>
    public async Task CheckMissingBreakAsync(
        string tenantId, Guid employeeId, decimal shiftHours, int breakMinutes, DateTime shiftDate, CancellationToken ct = default)
    {
        const decimal shiftThreshold = 6m;
        const int breakMinimumMinutes = 20;
        if (shiftHours <= shiftThreshold || breakMinutes >= breakMinimumMinutes) return;

        var policyCode = "POL-BREAK-001";
        var dayStart = shiftDate.Date;
        var dayEnd = dayStart.AddDays(1);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= dayStart
                     && v.DetectedAt < dayEnd)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            ShiftHours = shiftHours,
            BreakMinutes = breakMinutes,
            RequiredBreakMinutes = breakMinimumMinutes,
            ShiftDate = shiftDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.High, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Missing break violation: Employee {EmployeeId} worked {Hours:F1}h with {Break}min break on {Date:d}",
            employeeId, shiftHours, breakMinutes, shiftDate);
    }

    /// <summary>
    /// Checks for approval bypass: transaction processed without required approval.
    /// Policy code: POL-APPROVAL-001, Severity: Critical.
    /// </summary>
    public async Task CheckApprovalBypassAsync(
        string tenantId, Guid employeeId, string transactionType, string transactionId, DateTime referenceDate, CancellationToken ct = default)
    {
        var policyCode = "POL-APPROVAL-001";
        var dayStart = referenceDate.Date;
        var dayEnd = dayStart.AddDays(1);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= dayStart
                     && v.DetectedAt < dayEnd
                     && (v.EvidenceJson != null && v.EvidenceJson.Contains(transactionId)))
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            TransactionType = transactionType,
            TransactionId = transactionId,
            ReferenceDate = referenceDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.Critical, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Approval bypass violation: Employee {EmployeeId} {Type} {Id} on {Date:d}",
            employeeId, transactionType, transactionId, referenceDate);
    }

    /// <summary>
    /// Checks for rest period violation: less than 11 hours rest between consecutive shifts.
    /// Policy code: POL-REST-001, Severity: High.
    /// </summary>
    public async Task CheckRestPeriodViolationAsync(
        string tenantId, Guid employeeId, double restHoursBetweenShifts, DateTime shiftDate, CancellationToken ct = default)
    {
        const double minimumRestHours = 11.0;
        if (restHoursBetweenShifts >= minimumRestHours) return;

        var policyCode = "POL-REST-001";
        var dayStart = shiftDate.Date;
        var dayEnd = dayStart.AddDays(1);
        var existing = await _db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                     && v.EmployeeId == employeeId
                     && v.PolicyCode == policyCode
                     && v.Status == ViolationStatus.Open
                     && v.DetectedAt >= dayStart
                     && v.DetectedAt < dayEnd)
            .AnyAsync(ct);

        if (existing) return;

        var evidence = System.Text.Json.JsonSerializer.Serialize(new
        {
            RestHours = restHoursBetweenShifts,
            MinimumRestHours = minimumRestHours,
            ShiftDate = shiftDate
        });

        var violation = PolicyViolation.Detect(tenantId, policyCode, employeeId, ViolationSeverity.High, evidence);
        _db.PolicyViolations.Add(violation);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Rest period violation: Employee {EmployeeId} only {Hours:F1}h rest before shift on {Date:d}",
            employeeId, restHoursBetweenShifts, shiftDate);
    }
}
