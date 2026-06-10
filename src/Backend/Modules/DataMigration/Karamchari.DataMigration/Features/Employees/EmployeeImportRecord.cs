// -----------------------------------------------------------------------
// <copyright file="EmployeeImportRecord.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Features.Employees;

/// <summary>
/// Mapped representation of an employee record from an import file.
/// </summary>
public record EmployeeImportRecord
{
    public string? EmployeeNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? DepartmentName { get; init; }
    public string? DesignationName { get; init; }
    public string? ManagerEmployeeNumber { get; init; }
    public string? DateOfJoining { get; init; }
    public string? EmploymentType { get; init; }
    public string? BranchName { get; init; }
}
