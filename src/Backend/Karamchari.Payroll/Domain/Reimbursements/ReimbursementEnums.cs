namespace Karamchari.Payroll.Domain.Reimbursements;

/// <summary>
/// Authoritative business status for reimbursement claims.
/// Coordination states (Pending Approval, Under Review) are managed by Workflow.
/// </summary>
public enum ReimbursementStatus
{
    /// <summary>Claim is being drafted or evaluated.</summary>
    Draft = 1,

    /// <summary>Claim has been submitted and is awaiting coordination.</summary>
    Submitted = 2,

    /// <summary>Claim has been fully approved and is authoritative truth for payout.</summary>
    Approved = 3,

    /// <summary>Claim has been partially approved.</summary>
    PartiallyApproved = 4,

    /// <summary>Claim was rejected.</summary>
    Rejected = 5,

    /// <summary>Claim has been paid out in a payroll run.</summary>
    PaidOut = 6,

    /// <summary>Claim has been clawed back after payout.</summary>
    Clawback = 7
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ReimbursementCategory
{
    Travel,
    Meal,
    Internet,
    Fuel,
    Mobile,
    Relocation,
    FlexibleBenefit,
    Medical,
    Training,
    Other
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ReimbursementTaxability
{
    Taxable,
    Exempt,
    PartiallyTaxable
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FraudIndicatorLevel
{
    None,
    Low,
    Medium,
    High
}
