namespace Karamchari.DataMigration.Features.HistoricalPayroll;

public record HistoricalPayrollImportRecord
{
    public string? EmployeeNumber { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }
    public decimal? GrossPay { get; init; }
    public decimal? Deductions { get; init; }
    public decimal? NetPay { get; init; }
}
