// -----------------------------------------------------------------------
// <copyright file="EmployeeProfileDto.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Contracts.Employees;

/// <summary>
/// Enriched employee view: identity plus resolved organizational names
/// (department, designation, branch, reporting manager). Powers the employee
/// biography surface, which needs names rather than opaque foreign-key ids.
/// </summary>
public record EmployeeProfileDto(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn,
    string Status,
    string TimeZoneId,
    string EmploymentType,
    string? DepartmentName,
    string? DesignationName,
    int? DesignationLevel,
    string? BranchName,
    string? ManagerName);
