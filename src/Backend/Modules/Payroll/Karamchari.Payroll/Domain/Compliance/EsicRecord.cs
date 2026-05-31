namespace Karamchari.Payroll.Domain.Compliance;

/// <summary>
/// Domain model for the ESIC Monthly Contribution return.
/// </summary>
public sealed record EsicRecord
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InsuranceNumber { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GrossWages { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EmployeeContribution { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal EmployerContribution { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int DaysWorked { get; init; }
}
