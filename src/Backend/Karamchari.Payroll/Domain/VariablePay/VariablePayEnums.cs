namespace Karamchari.Payroll.Domain.VariablePay;

public enum VariablePayType
{
    PerformanceBonus,
    RetentionBonus,
    SalesIncentive,
    SpotBonus,
    JoiningBonus,
    QuarterlyIncentive,
    AnnualBonus,
    ProjectBonus,
    ReferralBonus
}

public enum VariablePayStatus
{
    Draft,
    PendingApproval,
    Approved,
    Scheduled,
    PaidOut,
    Clawback,
    Cancelled,
    Deferred
}

public enum TaxTreatment
{
    /// <summary>Spread across months for TDS calculation.</summary>
    Spread,
    /// <summary>Taxed fully in payout month.</summary>
    LumpSum
}
