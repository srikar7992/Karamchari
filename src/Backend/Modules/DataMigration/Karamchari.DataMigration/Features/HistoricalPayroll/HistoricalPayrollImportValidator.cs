using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.HistoricalPayroll;

public sealed class HistoricalPayrollImportValidator : IImportValidator<HistoricalPayrollImportRecord>
{
    private readonly IReferenceResolver _resolver;

    public HistoricalPayrollImportValidator(IReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ValidationResult> ValidateAsync(HistoricalPayrollImportRecord item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.EmployeeNumber))
            return new ValidationResult(false, "Employee Number is required.");

        if (item.Month is null or < 1 or > 12)
            return new ValidationResult(false, "Month must be between 1 and 12.");

        if (item.Year is null or < 2000 or > 2100)
            return new ValidationResult(false, "Year must be a valid calendar year.");

        if (item.NetPay is null)
            return new ValidationResult(false, "Net Pay is required.");

        var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber, ct);
        if (empId is null)
            return new ValidationResult(false, $"Employee '{item.EmployeeNumber}' not found.");

        return new ValidationResult(true);
    }
}
