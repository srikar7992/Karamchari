// -----------------------------------------------------------------------
// <copyright file="SalaryRevisionImportValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.SalaryRevision;

public sealed class SalaryRevisionImportValidator : IImportValidator<SalaryRevisionImportRecord>
{
    private readonly IReferenceResolver _resolver;

    public SalaryRevisionImportValidator(IReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ValidationResult> ValidateAsync(SalaryRevisionImportRecord item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.EmployeeNumber))
            return new ValidationResult(false, "Employee Number is required.");

        if (item.NewCTC is null or <= 0)
            return new ValidationResult(false, "New CTC must be a positive value.");

        if (!string.IsNullOrWhiteSpace(item.EffectiveDate) && !DateOnly.TryParse(item.EffectiveDate, out _))
            return new ValidationResult(false, $"Invalid Effective Date format: '{item.EffectiveDate}'.");

        var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber, ct);
        if (empId is null)
            return new ValidationResult(false, $"Employee '{item.EmployeeNumber}' not found.");

        return new ValidationResult(true);
    }
}
