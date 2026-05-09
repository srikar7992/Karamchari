namespace Karamchari.Payroll.Domain.Loans;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum LoanStatus
{
    PendingApproval,
    Active,
    Rejected,
    ClosedByEmployee,
    ClosedByExit,
    PreClosed,
    Restructured,
    Defaulted,
    Waived
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
