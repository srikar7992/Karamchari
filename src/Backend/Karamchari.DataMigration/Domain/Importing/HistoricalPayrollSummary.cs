using Karamchari.Core.Multitenancy;

namespace Karamchari.DataMigration.Domain.Importing;

/// <summary>
/// Stores an imported historical payroll summary for an employee.
/// Represents pre-system payroll data brought in for YTD calculations and reporting.
/// </summary>
public sealed class HistoricalPayrollSummary : ITenantOwned
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public Guid ImportJobId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal GrossPay { get; private set; }
    public decimal Deductions { get; private set; }
    public decimal NetPay { get; private set; }
    public DateTime ImportedAt { get; private set; }

    private HistoricalPayrollSummary() { }

    public static HistoricalPayrollSummary Create(
        Guid importJobId,
        string employeeNumber,
        int month,
        int year,
        decimal grossPay,
        decimal deductions,
        decimal netPay)
    {
        return new HistoricalPayrollSummary
        {
            Id = Guid.NewGuid(),
            ImportJobId = importJobId,
            EmployeeNumber = employeeNumber,
            Month = month,
            Year = year,
            GrossPay = grossPay,
            Deductions = deductions,
            NetPay = netPay,
            ImportedAt = DateTime.UtcNow
        };
    }
}
