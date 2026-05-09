namespace Karamchari.Payroll.Domain.FnF;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FnFExitType
{
    Resignation,
    Termination,
    Absconding,
    Retirement,
    ContractCompletion
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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
