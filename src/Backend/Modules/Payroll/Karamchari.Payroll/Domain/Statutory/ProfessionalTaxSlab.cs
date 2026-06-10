// -----------------------------------------------------------------------
// <copyright file="ProfessionalTaxSlab.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Statutory;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines a Professional Tax (PT) slab for a specific state and financial year.
/// </summary>
public class ProfessionalTaxSlab : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the state code (e.g., "TS", "MH", "KA").</summary>
    public string StateCode { get; private set; } = string.Empty;

    /// <summary>Gets the minimum monthly gross salary for this slab.</summary>
    public decimal MinGross { get; private set; }

    /// <summary>Gets the maximum monthly gross salary for this slab.</summary>
    public decimal MaxGross { get; private set; }

    /// <summary>Gets the monthly tax amount to be deducted.</summary>
    public decimal MonthlyTaxAmount { get; private set; }

    /// <summary>Gets the specific month this slab applies to (nullable, for months like February in MH).</summary>
    public int? ApplicableMonth { get; private set; }

    /// <summary>Gets the start year of the financial year.</summary>
    public int FinancialYearStart { get; private set; }

    /// <summary>Gets the end year of the financial year.</summary>
    public int FinancialYearEnd { get; private set; }

    /// <summary>Gets the priority for matching (higher priority wins if slabs overlap).</summary>
    public int Priority { get; private set; }

    /// <summary>Gets a value indicating whether the slab is active.</summary>
    public bool IsActive { get; private set; }

    private ProfessionalTaxSlab() { }

    /// <summary>
    /// Creates a new PT slab.
    /// </summary>
    public static ProfessionalTaxSlab Create(
        string stateCode,
        decimal minGross,
        decimal maxGross,
        decimal monthlyTaxAmount,
        int fyStart,
        int fyEnd,
        int? applicableMonth = null,
        int priority = 0)
    {
        return new ProfessionalTaxSlab
        {
            Id = Guid.NewGuid(),
            StateCode = stateCode,
            MinGross = minGross,
            MaxGross = maxGross,
            MonthlyTaxAmount = monthlyTaxAmount,
            FinancialYearStart = fyStart,
            FinancialYearEnd = fyEnd,
            ApplicableMonth = applicableMonth,
            Priority = priority,
            IsActive = true
        };
    }
}
