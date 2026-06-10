// -----------------------------------------------------------------------
// <copyright file="ComplianceGate.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Compliance.Contracts;

/// <summary>
/// Operations that can be blocked by an active compliance violation.
/// </summary>
public enum ComplianceOperation
{
    /// <summary>Adding an employee to a shift roster.</summary>
    Schedule,

    /// <summary>Assigning an asset or project task to an employee.</summary>
    Assign,

    /// <summary>Clocking in / starting a shift.</summary>
    StartShift,

    /// <summary>Processing payroll for an employee.</summary>
    ProcessPayroll,
}

/// <summary>
/// Describes a single active compliance block on an employee.
/// </summary>
public sealed record ComplianceBlock(
    string PolicyCode,
    string Reason,
    string Severity,
    DateTime DetectedAt);

/// <summary>
/// Result returned by <see cref="IComplianceGateService.CheckAsync"/>.
/// </summary>
public sealed record ComplianceGateResult(
    bool IsBlocked,
    IReadOnlyList<ComplianceBlock> Blocks)
{
    /// <summary>Convenience factory for a clean pass.</summary>
    public static ComplianceGateResult Allow() => new(false, []);

    /// <summary>Convenience factory for a blocked result.</summary>
    public static ComplianceGateResult Block(IReadOnlyList<ComplianceBlock> blocks) => new(true, blocks);
}

/// <summary>
/// Cross-module service that evaluates whether a compliance violation blocks a specific operation
/// for an employee. Implemented in <c>Karamchari.Compliance</c>; callable from any BFF handler
/// that holds a reference to <c>Karamchari.Compliance.Contracts</c>.
/// </summary>
public interface IComplianceGateService
{
    /// <summary>
    /// Returns a <see cref="ComplianceGateResult"/> indicating whether <paramref name="employeeId"/>
    /// is blocked from performing <paramref name="operation"/> due to open compliance violations.
    /// </summary>
    Task<ComplianceGateResult> CheckAsync(
        string tenantId,
        Guid employeeId,
        ComplianceOperation operation,
        CancellationToken cancellationToken = default);
}
