namespace Karamchari.DataMigration.Features.LeaveBalance;

public record LeaveBalanceImportRecord
{
    public string? EmployeeNumber { get; init; }
    public string? LeaveTypeName { get; init; }
    public decimal? Balance { get; init; }
    public string? EffectiveDate { get; init; }
}
