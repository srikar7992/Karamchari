namespace Karamchari.Payroll.Domain.Loans;

/// <summary>
/// Authoritative business status for employee loans.
/// Coordination states (Pending Approval) are managed by Workflow.
/// </summary>
public enum LoanStatus
{
    /// <summary>Loan is being drafted or awaiting coordination.</summary>
    Draft = 1,

    /// <summary>Loan has been approved and is authoritative truth for deduction.</summary>
    Active = 2,

    /// <summary>Loan was rejected.</summary>
    Rejected = 3,

    /// <summary>Loan has been fully repaid.</summary>
    Closed = 4,

    /// <summary>Loan was closed manually by the employee (e.g. foreclosure).</summary>
    ClosedByEmployee = 5,

    /// <summary>Loan was closed due to employee exit (settled in FnF).</summary>
    ClosedByExit = 6,

    /// <summary>Loan was pre-closed via lumpsum payment.</summary>
    PreClosed = 7,

    /// <summary>Loan terms were restructured.</summary>
    Restructured = 8,

    /// <summary>Loan has defaulted.</summary>
    Defaulted = 9,

    /// <summary>Remaining loan balance was waived by the company.</summary>
    Waived = 10
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum LoanType
{
    SalaryAdvance,
    EmployeeLoan,
    EmergencyLoan
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum LoanInterestType
{
    ZeroInterest,
    InterestBearing
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum InstallmentStatus
{
    Pending,
    Deducted,
    Skipped,        // payroll skipped, carry-forward
    CarriedForward,
    Waived
}
