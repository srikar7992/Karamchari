using Karamchari.FinancialOps.Contracts.Settlements;

namespace Karamchari.FinancialOps.Contracts.Reimbursements;

/// <summary>
/// A strongly-typed identifier for an expense claim.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record ExpenseClaimId(Guid Value)
{
    /// <summary>Creates a new unique identifier.</summary>
    public static ExpenseClaimId New() => new(Guid.NewGuid());
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// A strongly-typed identifier for an expense category.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record ExpenseCategoryId(Guid Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Defines the lifecycle states of an expense claim.
/// </summary>
public enum ExpenseClaimStatus
{
    /// <summary>Claim is being drafted.</summary>
    Draft = 1,
    /// <summary>Claim has been submitted for review.</summary>
    Submitted = 2,
    /// <summary>Claim is currently being reviewed.</summary>
    UnderReview = 3,
    /// <summary>Claim has been approved.</summary>
    Approved = 4,
    /// <summary>Claim has been rejected.</summary>
    Rejected = 5,
    /// <summary>Claim is queued for settlement.</summary>
    QueuedForSettlement = 6,
    /// <summary>Claim has been settled.</summary>
    Settled = 7,
    /// <summary>Claim has been reversed.</summary>
    Reversed = 8,
    /// <summary>Claim has been archived.</summary>
    Archived = 9
}

/// <summary>
/// Defines the tax treatment for a reimbursement.
/// </summary>
public enum TaxTreatment
{
    /// <summary>Reimbursement is added to taxable income in payroll.</summary>
    PayrollTaxable = 1,
    /// <summary>Reimbursement is fully exempt from tax.</summary>
    FullyExempt = 2,
    /// <summary>Tax exemption depends on employee declaration.</summary>
    DeclarationRequired = 3
}

/// <summary>
/// Defines types of operational holds that can be placed on a financial operation.
/// </summary>
public enum FinancialOperationalHoldType
{
    /// <summary>Hold for manual audit.</summary>
    Audit = 1,
    /// <summary>Hold due to suspicious activity detected.</summary>
    SuspiciousActivity = 2,
    /// <summary>Hold due to employee resignation process.</summary>
    Resignation = 3,
    /// <summary>Hold for compliance verification.</summary>
    Compliance = 4
}

/// <summary>
/// Command to submit a new expense claim.
/// </summary>
/// <param name="EmployeeId">The employee submitting the claim.</param>
/// <param name="CategoryId">The expense category.</param>
/// <param name="ClaimNumber">The unique claim number.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Currency">The currency of the claim.</param>
/// <param name="ExpenseDate">The date the expense was incurred.</param>
/// <param name="TaxTreatment">The tax treatment for the reimbursement.</param>
/// <param name="PreferredSettlementType">The preferred method of settlement.</param>
public record SubmitExpenseClaim(
    Guid EmployeeId,
    Guid CategoryId,
    string ClaimNumber,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    TaxTreatment TaxTreatment,
    SettlementType PreferredSettlementType);

/// <summary>
/// Event published when an expense claim is successfully submitted.
/// </summary>
/// <param name="ClaimId">The claim identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="EmployeeId">The employee identifier.</param>
/// <param name="Amount">The amount claimed.</param>
/// <param name="Currency">The currency code.</param>
public record ExpenseClaimSubmitted(
    Guid ClaimId,
    string TenantId,
    Guid EmployeeId,
    decimal Amount,
    string Currency);
