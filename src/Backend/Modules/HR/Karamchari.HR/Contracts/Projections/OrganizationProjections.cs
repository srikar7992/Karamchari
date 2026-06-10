// -----------------------------------------------------------------------
// <copyright file="OrganizationProjections.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Projections;

/// <summary>
/// View of an employee's organizational placement and reporting line.
/// </summary>
public sealed record EmployeeOrgProjection(
    Guid EmployeeId,
    string EmployeeName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DesignationId,
    string? DesignationName,
    Guid? ManagerId,
    string? ManagerName,
    Guid? BranchId,
    string? BranchName,
    Guid? LegalEntityId,
    string? LegalEntityName);

/// <summary>
/// Lightweight node for reporting hierarchy visualization.
/// </summary>
public sealed record ReportingHierarchyNode(
    Guid EmployeeId,
    string Name,
    string Designation,
    string? AvatarUri,
    List<ReportingHierarchyNode> DirectReports);

/// <summary>
/// Represents a position in the organization structure.
/// </summary>
public sealed record PositionProjection(
    Guid PositionId,
    string PositionCode,
    string DesignationName,
    string DepartmentName,
    Guid? IncumbentEmployeeId,
    string? IncumbentName,
    Guid? ReportingToPositionId);
