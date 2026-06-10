// -----------------------------------------------------------------------
// <copyright file="LoanInstallment.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Loans;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class LoanInstallment
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int InstallmentNumber { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PeriodName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int Year { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int Month { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PrincipalAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal InterestAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalAmount => PrincipalAmount + InterestAmount;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal OutstandingAfter { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public InstallmentStatus Status { get; private set; }
    public DateTimeOffset? DeductedAtUtc { get; private set; }
    public string? SkipReason { get; private set; }

    private LoanInstallment() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static LoanInstallment Create(
        int number,
        string periodName,
        int year,
        int month,
        decimal principal,
        decimal interest,
        decimal outstandingAfter)
    {
        return new LoanInstallment
        {
            Id = Guid.NewGuid(),
            InstallmentNumber = number,
            PeriodName = periodName,
            Year = year,
            Month = month,
            PrincipalAmount = principal,
            InterestAmount = interest,
            OutstandingAfter = outstandingAfter,
            Status = InstallmentStatus.Pending
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkDeducted()
    {
        Status = InstallmentStatus.Deducted;
        DeductedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Skip(string reason)
    {
        Status = InstallmentStatus.Skipped;
        SkipReason = reason;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void CarryForward()
    {
        Status = InstallmentStatus.CarriedForward;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Waive()
    {
        Status = InstallmentStatus.Waived;
    }
}
