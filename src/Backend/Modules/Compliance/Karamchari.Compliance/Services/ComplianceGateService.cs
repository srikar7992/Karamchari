// -----------------------------------------------------------------------
// <copyright file="ComplianceGateService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compliance.Contracts;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Evaluates active policy violations to determine whether an employee is blocked from
/// a specific platform operation.
///
/// Blocking rules:
/// - Critical severity violations block ALL operations.
/// - High severity violations block: Schedule, Assign, StartShift.
/// - Medium severity violations block: Schedule, StartShift.
/// - Low severity violations: informational only — no block.
/// - AcceptedRisk and Resolved violations never block.
///
/// Operations and their minimum blocking severities:
///   Schedule    → Medium+
///   StartShift  → Medium+
///   Assign      → High+
///   ProcessPayroll → Critical only
/// </summary>
public sealed class ComplianceGateService(ComplianceDbContext db) : IComplianceGateService
{
    public async Task<ComplianceGateResult> CheckAsync(
        string tenantId,
        Guid employeeId,
        ComplianceOperation operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Fetch all active (non-resolved) violations for the employee.
        var activeViolations = await db.PolicyViolations
            .Where(v => v.TenantId == tenantId
                && v.EmployeeId == employeeId
                && (v.Status == ViolationStatus.Open || v.Status == ViolationStatus.Investigating))
            .Select(v => new { v.PolicyCode, v.Severity, v.DetectedAt, v.Notes })
            .ToListAsync(cancellationToken);

        if (activeViolations.Count == 0)
            return ComplianceGateResult.Allow();

        // Filter to violations that block this specific operation.
        var blocking = activeViolations
            .Where(v => Blocks(v.Severity, operation))
            .Select(v => new ComplianceBlock(
                v.PolicyCode,
                BuildReason(v.Severity, operation, v.Notes),
                v.Severity.ToString(),
                v.DetectedAt))
            .ToList();

        return blocking.Count == 0
            ? ComplianceGateResult.Allow()
            : ComplianceGateResult.Block(blocking);
    }

    private static bool Blocks(ViolationSeverity severity, ComplianceOperation operation) =>
        operation switch
        {
            ComplianceOperation.Schedule => severity >= ViolationSeverity.Medium,
            ComplianceOperation.StartShift => severity >= ViolationSeverity.Medium,
            ComplianceOperation.Assign => severity >= ViolationSeverity.High,
            ComplianceOperation.ProcessPayroll => severity >= ViolationSeverity.Critical,
            _ => false,
        };

    private static string BuildReason(ViolationSeverity severity, ComplianceOperation operation, string? notes)
    {
        var operationLabel = operation switch
        {
            ComplianceOperation.Schedule => "scheduling",
            ComplianceOperation.StartShift => "starting a shift",
            ComplianceOperation.Assign => "assignment",
            ComplianceOperation.ProcessPayroll => "payroll processing",
            _ => "this operation",
        };
        var base_ = $"Active {severity} compliance violation blocks {operationLabel}.";
        return string.IsNullOrWhiteSpace(notes) ? base_ : $"{base_} Detail: {notes}";
    }
}
