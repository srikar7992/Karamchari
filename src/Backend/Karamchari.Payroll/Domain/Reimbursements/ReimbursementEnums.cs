namespace Karamchari.Payroll.Domain.Reimbursements;

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

public enum ReimbursementTaxability
{
    Exempt,
    Taxable,
    PartiallyTaxable
}

public enum FraudIndicatorLevel
{
    None,
    Low,
    Medium,
    High
}
