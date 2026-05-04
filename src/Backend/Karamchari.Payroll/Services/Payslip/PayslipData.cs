namespace Karamchari.Payroll.Services.Payslip;

/// <summary>
/// A flattened, immutable data structure for generating a payslip.
/// Decouples the PDF generation from the complex domain logic.
/// </summary>
/// <param name="EmployeeName">Legal name of the employee.</param>
/// <param name="EmployeeId">Unique employee identifier (Work ID).</param>
/// <param name="Month">The month name (e.g., "April 2027").</param>
/// <param name="Gross">Total earnings before deductions.</param>
/// <param name="NetPay">Final take-home amount.</param>
/// <param name="Earnings">List of all earning components (Basic, HRA, etc.).</param>
/// <param name="Deductions">List of all deductions (EPF, ESIC, PT, TDS).</param>
/// <param name="YtdTotals">Year-To-Date totals for key components.</param>
/// <param name="TaxRegime">Selected tax regime (Old/New).</param>
/// <param name="Designation">Employee's job title.</param>
/// <param name="Department">Employee's department.</param>
/// <param name="Pan">Permanent Account Number (Indian Tax ID).</param>
/// <param name="Aadhaar">Aadhaar number (Optional).</param>
/// <param name="BankName">Bank where salary is credited.</param>
/// <param name="AccountNumber">Masked bank account number.</param>
public record PayslipData(
    string EmployeeName,
    string EmployeeId,
    string Month,
    decimal Gross,
    decimal NetPay,
    IReadOnlyDictionary<string, decimal> Earnings,
    IReadOnlyDictionary<string, decimal> Deductions,
    IReadOnlyDictionary<string, decimal> YtdTotals,
    string TaxRegime,
    string Designation = "Software Engineer",
    string Department = "Engineering",
    string Pan = "ABCDE1234F",
    string Aadhaar = "XXXX-XXXX-1234",
    string BankName = "HDFC Bank",
    string AccountNumber = "XXXXXXX1234"
);
