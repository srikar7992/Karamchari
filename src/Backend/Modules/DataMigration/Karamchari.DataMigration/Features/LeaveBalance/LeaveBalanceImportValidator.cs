using Karamchari.DataMigration.Services;

namespace Karamchari.DataMigration.Features.LeaveBalance;

public sealed class LeaveBalanceImportValidator : IImportValidator<LeaveBalanceImportRecord>
{
    private readonly IReferenceResolver _resolver;

    public LeaveBalanceImportValidator(IReferenceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<ValidationResult> ValidateAsync(LeaveBalanceImportRecord item, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(item.EmployeeNumber))
            return new ValidationResult(false, "Employee Number is required.");

        if (string.IsNullOrWhiteSpace(item.LeaveTypeName))
            return new ValidationResult(false, "Leave Type is required.");

        if (item.Balance is null)
            return new ValidationResult(false, "Balance is required.");

        var empId = await _resolver.ResolveEmployeeIdAsync(item.EmployeeNumber, ct);
        if (empId is null)
            return new ValidationResult(false, $"Employee '{item.EmployeeNumber}' not found.");

        var policyId = await _resolver.ResolveLeavePolicyIdAsync(item.LeaveTypeName, ct);
        if (policyId is null)
            return new ValidationResult(false, $"Leave type '{item.LeaveTypeName}' not found.");

        if (!string.IsNullOrWhiteSpace(item.EffectiveDate) && !DateOnly.TryParse(item.EffectiveDate, out _))
            return new ValidationResult(false, $"Invalid Effective Date format: '{item.EffectiveDate}'.");

        return new ValidationResult(true);
    }
}
