// -----------------------------------------------------------------------
// <copyright file="EmployeeProjections.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Projections;

/// <summary>
/// Projection used for workforce identity and basic organizational context.
/// Consumed by Attendance, Payroll, and Workflow.
/// </summary>
public sealed record EmployeeSummaryProjection(
    Guid EmployeeId,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    string EmploymentStatus,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DesignationId,
    string? DesignationName,
    Guid? ReportingManagerId,
    string? ReportingManagerName,
    string TimeZoneId);

/// <summary>
/// Minimal projection for payroll processing.
/// </summary>
public sealed record EmployeePayrollProjection(
    Guid EmployeeId,
    string EmployeeNumber,
    string LegalName,
    string EmploymentType,
    Guid? LegalEntityId,
    Guid? BranchId,
    string? BankAccountDetails, // Conceptual
    bool IsActive);
