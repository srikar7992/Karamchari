// -----------------------------------------------------------------------
// <copyright file="EsiCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.DeductionRules;

namespace Karamchari.Payroll.Services.Deductions;

/// <summary>
/// ESI: 0.75% employee, 3.25% employer on gross ≤ ₹21,000.
/// Once IsEsicLocked = true for the contribution period, deduction stops.
/// </summary>
public sealed class EsiCalculator : IDeductionCalculator
{
    private const decimal GrossCeiling = 21_000m;
    private const decimal EmployeeRate = 0.0075m;
    private const decimal EmployerRate = 0.0325m;

    public StatutoryDeductionType DeductionType => StatutoryDeductionType.EmployeeStateInsurance;

    public decimal Calculate(DeductionContext context)
    {
        if (context.IsEsicLocked) return 0m;
        if (context.GrossPay > GrossCeiling) return 0m;

        return Math.Round(context.GrossPay * EmployeeRate, 0);
    }

    public decimal EmployerContribution(DeductionContext context)
    {
        if (context.IsEsicLocked) return 0m;
        if (context.GrossPay > GrossCeiling) return 0m;

        return Math.Round(context.GrossPay * EmployerRate, 0);
    }
}
