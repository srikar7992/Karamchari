namespace Karamchari.Payroll.Domain.VariablePay;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum TaxTreatment
{
    /// <summary>Spread across months for TDS calculation.</summary>
    Spread,
    /// <summary>Taxed fully in payout month.</summary>
    LumpSum
}
