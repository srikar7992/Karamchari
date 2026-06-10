// -----------------------------------------------------------------------
// <copyright file="IDeductionCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.DeductionRules;

/// <summary>
/// Input context for deduction calculation.
/// </summary>
public sealed record DeductionContext(
    Guid EmployeeId,
    string TenantId,
    decimal GrossPay,
    decimal BasicPay,
    decimal DaAmount,
    bool OptedVoluntaryPf,
    bool IsEsicLocked,
    string StateCode,
    int Month,
    int FinancialYear);

/// <summary>
/// Strategy interface. Each statutory deduction type gets its own implementation.
/// Prevents the single-method Calculate() from becoming a monster.
/// </summary>
public interface IDeductionCalculator
{
    StatutoryDeductionType DeductionType { get; }

    /// <summary>Returns the calculated deduction amount. Returns 0 if not applicable.</summary>
    decimal Calculate(DeductionContext context);

    /// <summary>Returns employer contribution where applicable (PF, ESI). Returns 0 otherwise.</summary>
    decimal EmployerContribution(DeductionContext context) => 0m;
}
