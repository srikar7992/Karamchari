namespace Karamchari.Payroll.Domain.Arrears;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ArrearStatus
{
    Pending,
    PendingApproval,
    Approved,
    Processed,
    Reversed,
    Cancelled
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ArrearTriggerType
{
    SalaryRevision,
    AttendanceCorrection,
    ShiftCorrection,
    OvertimeCorrection,
    TaxRecalculation,
    BackdatedPromotion,
    ComplianceAdjustment,
    LeaveCorrection,
    ManualAdjustment
}
