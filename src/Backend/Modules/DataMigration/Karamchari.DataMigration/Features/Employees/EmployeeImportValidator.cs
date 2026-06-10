// -----------------------------------------------------------------------
// <copyright file="EmployeeImportValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.Employees;

/// <summary>
/// Implementation of <see cref="IImportValidator{T}"/> for Employee records.
/// Performs business validation and reference checking.
/// </summary>
public sealed class EmployeeImportValidator : IImportValidator<EmployeeImportRecord>
{
    private readonly IReferenceResolver _referenceResolver;

    public EmployeeImportValidator(IReferenceResolver referenceResolver)
    {
        _referenceResolver = referenceResolver;
    }

    public async Task<ValidationResult> ValidateAsync(EmployeeImportRecord item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.EmployeeNumber))
            return new ValidationResult(false, "Employee Number is required.");

        if (string.IsNullOrWhiteSpace(item.Email))
            return new ValidationResult(false, "Email is required.");

        if (string.IsNullOrWhiteSpace(item.FirstName))
            return new ValidationResult(false, "First Name is required.");

        if (!string.IsNullOrWhiteSpace(item.DepartmentName))
        {
            var deptId = await _referenceResolver.ResolveDepartmentIdAsync(item.DepartmentName, ct);
            if (deptId == null)
                return new ValidationResult(false, $"Department '{item.DepartmentName}' could not be resolved.");
        }

        if (!string.IsNullOrWhiteSpace(item.ManagerEmployeeNumber))
        {
            var managerId = await _referenceResolver.ResolveEmployeeIdAsync(item.ManagerEmployeeNumber, ct);
            if (managerId == null)
                return new ValidationResult(false, $"Manager with Employee Number '{item.ManagerEmployeeNumber}' could not be resolved.");
        }

        if (!string.IsNullOrWhiteSpace(item.DateOfJoining))
        {
            if (!DateOnly.TryParse(item.DateOfJoining, out _))
                return new ValidationResult(false, $"Invalid Date of Joining format: '{item.DateOfJoining}'. Expected YYYY-MM-DD.");
        }

        return new ValidationResult(true);
    }
}
