namespace Karamchari.Core.Contracts;

/// <summary>
/// Fired when an individual employee's payroll calculation is finalized.
/// </summary>
/// <param name="EmployeeId">The employee identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PeriodName">The payroll month (e.g., "April 2027").</param>
/// <param name="Gross">The total gross calculated.</param>
/// <param name="NetPay">The net pay calculated.</param>
/// <param name="Earnings">Final earnings breakdown.</param>
/// <param name="Deductions">Final statutory and other deductions.</param>
/// <param name="TaxRegime">The tax regime used for this run.</param>
public record PayrollRunCompletedIntegrationEvent(
    Guid EmployeeId,
    string TenantId,
    string PeriodName,
    decimal Gross,
    decimal NetPay,
    Dictionary<string, decimal> Earnings,
    Dictionary<string, decimal> Deductions,
    string TaxRegime
);

/// <summary>
/// Fired when an entire payroll run is locked by an admin.
/// Triggers the mass-generation of payslips.
/// </summary>
/// <param name="RunId">The payroll run identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PeriodName">The payroll month.</param>
public record PayrollRunLockedIntegrationEvent(
    Guid RunId,
    string TenantId,
    string PeriodName
);
