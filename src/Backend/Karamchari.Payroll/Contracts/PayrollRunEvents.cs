namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Command to initiate a new payroll run for a specific tenant and period.
/// </summary>
/// <param name="RunId">The unique identifier for the run.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="PeriodName">The name of the payroll period.</param>
public record StartPayrollRunCommand(Guid RunId, string TenantId, string PeriodName);

/// <summary>
/// Event published when an individual employee's pay has been calculated.
/// </summary>
/// <param name="RunId">The associated payroll run identifier.</param>
/// <param name="EmployeeId">The employee identifier.</param>
/// <param name="NetPay">The final calculated net pay after deductions.</param>
public record EmployeePayCalculatedEvent(Guid RunId, Guid EmployeeId, decimal NetPay);

/// <summary>
/// Command to approve and finalize a payroll run.
/// </summary>
/// <param name="RunId">The payroll run identifier.</param>
public record PayrollRunApprovedCommand(Guid RunId);

/// <summary>
/// Command to trigger the calculation of pay for all active employees in a run.
/// </summary>
/// <param name="RunId">The associated payroll run identifier.</param>
/// <param name="PeriodName">The name of the payroll period (used to query localized deductions).</param>
public record CalculateAllEmployeePayCommand(Guid RunId, string PeriodName);
