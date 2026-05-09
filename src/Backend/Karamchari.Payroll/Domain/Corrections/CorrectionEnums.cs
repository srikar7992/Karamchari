namespace Karamchari.Payroll.Domain.Corrections;

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

public enum CorrectionScope
{
    /// <summary>Differential adjusted in next payroll cycle.</summary>
    DifferentialAdjustment,
    /// <summary>Payroll fully reprocessed for affected period.</summary>
    FullReprocess
}
