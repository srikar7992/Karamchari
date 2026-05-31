namespace Karamchari.Payroll.Services.Payslip;

/// <summary>
/// Defines a service for generating a PDF payslip from data.
/// </summary>
public interface IPayslipGenerator
{
    /// <summary>
    /// Generates a PDF byte array from the provided payslip data.
    /// </summary>
    byte[] Generate(PayslipData data);
}

/// <summary>
/// Defines a service for persisting generated payslips.
/// </summary>
public interface IPayslipStorage
{
    /// <summary>
    /// Saves the payslip PDF to storage.
    /// </summary>
    Task<string> SaveAsync(string employeeId, string periodName, byte[] pdfBytes);

    /// <summary>
    /// Retrieves the payslip PDF from storage.
    /// </summary>
    Task<byte[]> GetAsync(string employeeId, string periodName);

    /// <summary>
    /// Returns <c>true</c> if a payslip has already been stored for this employee and period.
    /// Used for idempotency checks in the payslip generation consumer.
    /// </summary>
    Task<bool> ExistsAsync(string employeeId, string periodName);
}
