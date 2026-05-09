namespace Karamchari.Payroll.Domain.Corrections;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CorrectionStatus
{
    Draft,
    PendingApproval,
    Approved,
    RecalculationInProgress,
    Processed,
    Rejected,
    Cancelled
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CorrectionType
{
    SalaryChange,
    AttendanceFix,
    LeaveCorrection,
    ReimbursementCorrection,
    WrongPayrollRecovery,
    ComplianceCorrection,
    TaxCorrection,
    DeductionCorrection,
    OvertimeCorrection,
    BonusCorrection
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CorrectionScope
{
    /// <summary>Differential adjusted in next payroll cycle.</summary>
    DifferentialAdjustment,
    /// <summary>Payroll fully reprocessed for affected period.</summary>
    FullReprocess
}
