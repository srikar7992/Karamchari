// -----------------------------------------------------------------------
// <copyright file="EmployeeImportMapper.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.Employees;

/// <summary>
/// Implementation of <see cref="IImportMapper{T}"/> for Employee records.
/// </summary>
public sealed class EmployeeImportMapper : IImportMapper<EmployeeImportRecord>
{
    public EmployeeImportRecord Map(ImportRow row)
    {
        return new EmployeeImportRecord
        {
            EmployeeNumber = row.Data.GetValueOrDefault("employee number") ?? row.Data.GetValueOrDefault("emp id"),
            FirstName = row.Data.GetValueOrDefault("first name") ?? row.Data.GetValueOrDefault("fname"),
            LastName = row.Data.GetValueOrDefault("last name") ?? row.Data.GetValueOrDefault("lname"),
            Email = row.Data.GetValueOrDefault("email"),
            DepartmentName = row.Data.GetValueOrDefault("department") ?? row.Data.GetValueOrDefault("dept"),
            DesignationName = row.Data.GetValueOrDefault("designation") ?? row.Data.GetValueOrDefault("title"),
            ManagerEmployeeNumber = row.Data.GetValueOrDefault("manager employee number") ?? row.Data.GetValueOrDefault("mgr id"),
            DateOfJoining = row.Data.GetValueOrDefault("date of joining") ?? row.Data.GetValueOrDefault("doj"),
            EmploymentType = row.Data.GetValueOrDefault("employment type") ?? row.Data.GetValueOrDefault("type"),
            BranchName = row.Data.GetValueOrDefault("branch")
        };
    }
}
