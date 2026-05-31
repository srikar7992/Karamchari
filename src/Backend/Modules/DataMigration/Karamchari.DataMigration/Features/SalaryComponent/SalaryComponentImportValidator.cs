using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.SalaryComponent;

public sealed class SalaryComponentImportValidator : IImportValidator<SalaryComponentImportRecord>
{
    private readonly IReferenceResolver _resolver;

    public SalaryComponentImportValidator(IReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ValidationResult> ValidateAsync(SalaryComponentImportRecord item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.EmployeeNumber))
            return new ValidationResult(false, "Employee Number is required.");

        if (string.IsNullOrWhiteSpace(item.ComponentName))
            return new ValidationResult(false, "Component name is required.");

        if (item.Amount is null or <= 0)
            return new ValidationResult(false, "Amount must be a positive value.");

        var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber, ct);
        if (empId is null)
            return new ValidationResult(false, $"Employee '{item.EmployeeNumber}' not found.");

        return new ValidationResult(true);
    }
}
