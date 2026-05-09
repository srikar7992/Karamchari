namespace Karamchari.Payroll.Domain.Loans;

public enum LoanStatus
{
    Active,
    ClosedByEmployee,
    ClosedByExit,
    PreClosed,
    Restructured,
    Defaulted,
    Waived
}

public enum LoanType
{
    SalaryAdvance,
    EmployeeLoan,
    EmergencyLoan
}

public enum LoanInterestType
{
    ZeroInterest,
    InterestBearing
}

public enum InstallmentStatus
{
    Pending,
    Deducted,
    Skipped,        // payroll skipped, carry-forward
    CarriedForward,
    Waived
}
