namespace Karamchari.Payroll.Domain.Reimbursements;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ReimbursementStatus
{
    Draft,
    Submitted,
    PendingApproval,
    PartiallyApproved,
    Approved,
    Rejected,
    PaidOut,
    Clawback
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
    Exempt,
    Taxable,
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
