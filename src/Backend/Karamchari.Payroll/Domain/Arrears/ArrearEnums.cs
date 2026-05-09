namespace Karamchari.Payroll.Domain.Arrears;

public enum ArrearStatus
{
    Pending,
    PendingApproval,
    Approved,
    Processed,
    Reversed,
    Cancelled
}

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
