// -----------------------------------------------------------------------
// <copyright file="EmployeeLoan.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Aggregate root for an employee loan.
/// Tracks principal, interest, and repayment progress with payroll-safe integration.
/// </summary>
public sealed class EmployeeLoan : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LoanRepaymentEntry> _repayments = [];

    private EmployeeLoan(
        Guid id,
        string tenantId,
        EmployeeId employeeId,
        decimal principalAmount,
        decimal interestRate,
        int tenureMonths,
        FinancialReplayMetadata replayMetadata)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        PrincipalAmount = principalAmount;
        InterestRate = interestRate;
        TenureMonths = tenureMonths;
        RemainingPrincipal = principalAmount;
        ReplayMetadata = replayMetadata;
        Status = LoanStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private EmployeeLoan()
    {
        // EF Core
        TenantId = string.Empty;
        EmployeeId = null!;
        ReplayMetadata = null!;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the employee identifier.</summary>
    public EmployeeId EmployeeId { get; private set; }

    /// <summary>Gets the principal amount of the loan.</summary>
    public decimal PrincipalAmount { get; private set; }

    /// <summary>Gets the interest rate (annual percentage).</summary>
    public decimal InterestRate { get; private set; }

    /// <summary>Gets the tenure in months.</summary>
    public int TenureMonths { get; private set; }

    /// <summary>Gets the remaining principal balance.</summary>
    public decimal RemainingPrincipal { get; private set; }

    /// <summary>Gets the current status of the loan.</summary>
    public LoanStatus Status { get; private set; }

    /// <summary>Gets the replay metadata.</summary>
    public FinancialReplayMetadata ReplayMetadata { get; private set; }

    /// <summary>Gets the timestamp when the loan was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the collection of repayments made against this loan.</summary>
    public IReadOnlyCollection<LoanRepaymentEntry> Repayments => _repayments.AsReadOnly();

    /// <summary>Creates a new employee loan.</summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="employeeId">Employee ID.</param>
    /// <param name="principalAmount">Principal.</param>
    /// <param name="interestRate">Rate.</param>
    /// <param name="tenureMonths">Months.</param>
    /// <param name="replayMetadata">Metadata.</param>
    /// <returns>A new loan instance.</returns>
    public static EmployeeLoan Create(
        string tenantId,
        EmployeeId employeeId,
        decimal principalAmount,
        decimal interestRate,
        int tenureMonths,
        FinancialReplayMetadata? replayMetadata = null)
    {
        return new EmployeeLoan(
            Guid.NewGuid(),
            tenantId,
            employeeId,
            principalAmount,
            interestRate,
            tenureMonths,
            replayMetadata ?? FinancialReplayMetadata.Empty);
    }

    /// <summary>Records a repayment against the loan principal.</summary>
    /// <param name="amount">Amount.</param>
    /// <param name="explanation">Reason.</param>
    /// <param name="correlationChain">Chain.</param>
    public void RecordRepayment(decimal amount, string explanation, FinancialCorrelationChain correlationChain)
    {
        if (amount <= 0) throw new ArgumentException("Repayment amount must be positive.");
        if (amount > RemainingPrincipal) amount = RemainingPrincipal;

        RemainingPrincipal -= amount;
        _repayments.Add(new LoanRepaymentEntry(Guid.NewGuid(), amount, DateTimeOffset.UtcNow, explanation, correlationChain));

        if (RemainingPrincipal == 0)
        {
            Status = LoanStatus.Closed;
        }
    }
}

/// <summary>Defines the lifecycle states of an employee loan.</summary>
public enum LoanStatus
{
    /// <summary>Loan is active and being repaid.</summary>
    Active = 1,
    /// <summary>Loan has been fully repaid.</summary>
    Closed = 2,
    /// <summary>Loan has been written off.</summary>
    WrittenOff = 3,
    /// <summary>Loan is in default.</summary>
    Defaulted = 4
}

/// <summary>Represents a repayment entry for a loan.</summary>
public sealed class LoanRepaymentEntry
{
    private LoanRepaymentEntry()
    {
        // EF Core
        Explanation = string.Empty;
        CorrelationChain = null!;
    }

    internal LoanRepaymentEntry(Guid id, decimal amount, DateTimeOffset repaidAt, string explanation, FinancialCorrelationChain correlationChain)
    {
        Id = id;
        Amount = amount;
        RepaidAt = repaidAt;
        Explanation = explanation;
        CorrelationChain = correlationChain;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }
    /// <summary>Gets the repaid amount.</summary>
    public decimal Amount { get; private set; }
    /// <summary>Gets the timestamp.</summary>
    public DateTimeOffset RepaidAt { get; private set; }
    /// <summary>Gets the explanation.</summary>
    public string Explanation { get; private set; }
    /// <summary>Gets the correlation chain.</summary>
    public FinancialCorrelationChain CorrelationChain { get; private set; }
}
