namespace Karamchari.Payroll.Domain.FnF;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ExitType
{
    Resignation,
    Termination,
    Retirement,
    DeathInService
}

/// <summary>
/// Authoritative business status for FnF settlements.
/// </summary>
public enum FnFStatus
{
    /// <summary>Settlement is being drafted.</summary>
    Draft = 1,

    /// <summary>Settlement has been submitted and is awaiting coordination.</summary>
    Submitted = 2,

    /// <summary>Settlement has been approved and is authoritative truth.</summary>
    Approved = 3,

    /// <summary>Settlement has been disbursed.</summary>
    Disbursed = 4,

    /// <summary>Settlement was cancelled.</summary>
    Cancelled = 5,

    /// <summary>Settlement is on hold due to pending clearance.</summary>
    OnHold = 6,

    /// <summary>Settlement was reopened for recalculation.</summary>
    Reopened = 7
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FnFLineItemType
{
    Gratuity,
    LeaveEncashment,
    NoticePay,
    Severance,
    Bonus,
    Recovery,
    PendingSalary,
    NoticePeriodRecovery,
    LoanRecovery,
    Other
}
