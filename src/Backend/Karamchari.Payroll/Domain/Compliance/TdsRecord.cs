namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model for the TDS Quarterly Return (Form 24Q).
/// </summary>
public sealed record TdsRecord
{
    public string Pan { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public decimal AnnualIncome { get; init; }
    public decimal TotalTax { get; init; }
    public decimal TdsDeducted { get; init; }
    public decimal RemainingTax { get; init; }
}
