namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model for the ESIC Monthly Contribution return.
/// </summary>
public sealed record EsicRecord
{
    public string InsuranceNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public decimal GrossWages { get; init; }
    public decimal EmployeeContribution { get; init; }
    public decimal EmployerContribution { get; init; }
    public int DaysWorked { get; init; }
}
