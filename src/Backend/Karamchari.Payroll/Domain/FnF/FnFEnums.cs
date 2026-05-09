namespace Karamchari.Payroll.Domain.FnF;

public enum FnFExitType
{
    Resignation,
    Termination,
    Absconding,
    Retirement,
    ContractCompletion
}

public enum FnFStatus
{
    Draft,
    PendingApproval,
    Approved,
    Disbursed,
    Cancelled,
    OnHold,        // legal hold
    Reopened,
    PostFnFCorrection
}

public enum FnFLineItemType
{
    PendingSalary,
    LeaveEncashment,
    Gratuity,
    BonusAdjustment,
    ReimbursementSettlement,
    LoanRecovery,
    NoticePeriodRecovery,
    AssetRecovery,
    TaxAdjustment,
    ComplianceDeduction,
    VariablePaySettlement,
    ArrearsAdjustment,
    Other
}
