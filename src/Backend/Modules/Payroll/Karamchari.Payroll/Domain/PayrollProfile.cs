// -----------------------------------------------------------------------
// <copyright file="PayrollProfile.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines how an employee is compensated (Fixed vs Variable).
/// </summary>
public enum PayType
{
    /// <summary>Fixed monthly salary regardless of hours worked.</summary>
    Salaried = 0,

    /// <summary>Compensation based on approved billable hours.</summary>
    Hourly = 1
}

/// <summary>
/// Domain model for an employee's payroll configuration and compensation rules.
/// Country-specific statutory fields live in separate profiles (e.g., <see cref="IndiaPayrollProfile"/>)
/// so that a UAE employee can have a <see cref="PayrollProfile"/> with no India fields at all.
/// </summary>
public class PayrollProfile : ITenantOwned
{
    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier for the profile.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the employee identifier (soft reference to HR Domain).</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the cached employee name for reporting and payslips.</summary>
    public string EmployeeName { get; private set; } = string.Empty;

    /// <summary>Gets the compensation type.</summary>
    public PayType PayType { get; private set; }

    /// <summary>Gets the fixed monthly base salary. Only used if PayType is Salaried.</summary>
    public decimal BaseSalary { get; private set; }

    /// <summary>Gets the hourly rate. Only used if PayType is Hourly.</summary>
    public decimal HourlyRate { get; private set; }

    /// <summary>Gets the currency code (e.g., "INR", "USD").</summary>
    public string Currency { get; private set; }

    /// <summary>Gets a value indicating whether the profile is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the Total Annual CTC.</summary>
    public decimal AnnualCTC { get; private set; }

    /// <summary>Gets the assigned Salary Template ID.</summary>
    public Guid SalaryTemplateId { get; private set; }

    /// <summary>
    /// Gets India-specific statutory configuration.
    /// Null for employees not subject to Indian labour law.
    /// </summary>
    public IndiaPayrollProfile? India { get; private set; }

    /// <summary>
    /// Assigns the compensation configuration for this profile.
    /// Called when HR configures payroll for an employee (salary template assignment or revision approval).
    /// </summary>
    public void AssignCompensation(decimal annualCtc, Guid salaryTemplateId)
    {
        if (annualCtc <= 0) throw new ArgumentOutOfRangeException(nameof(annualCtc), "Annual CTC must be positive.");
        if (salaryTemplateId == Guid.Empty) throw new ArgumentException("Salary template ID cannot be empty.", nameof(salaryTemplateId));
        AnnualCTC = annualCtc;
        SalaryTemplateId = salaryTemplateId;
    }

    /// <summary>
    /// Attaches an India statutory profile to this payroll profile.
    /// Called once when the employee is confirmed to be processed under Indian labour law.
    /// </summary>
    public void AttachIndiaProfile(IndiaPayrollProfile india)
    {
        ArgumentNullException.ThrowIfNull(india);
        India = india;
    }

    /// <summary>
    /// Updates the India statutory identifiers (PAN, UAN, ESIC Number).
    /// </summary>
    public void UpdateStatutoryInfo(string pan, string uan, string esicNumber)
    {
        if (India is null)
            throw new InvalidOperationException("Cannot update statutory identifiers: no India payroll profile is attached.");
        India.UpdateIdentifiers(pan, uan, esicNumber);
    }

    /// <summary>
    /// Creates a shallow clone with an overridden tax regime (for simulations).
    /// The India profile (if present) is cloned with the new regime via <see cref="IndiaPayrollProfile.CloneWithRegime"/>.
    /// </summary>
    public PayrollProfile CloneWithRegime(TaxRegime regime)
    {
        var clone = new PayrollProfile
        {
            Id = this.Id,
            EmployeeId = this.EmployeeId,
            EmployeeName = this.EmployeeName,
            TenantId = this.TenantId,
            PayType = this.PayType,
            BaseSalary = this.BaseSalary,
            HourlyRate = this.HourlyRate,
            Currency = this.Currency,
            IsActive = this.IsActive,
            AnnualCTC = this.AnnualCTC,
            SalaryTemplateId = this.SalaryTemplateId,
            India = this.India?.CloneWithRegime(regime)
        };
        return clone;
    }

    private PayrollProfile()
    {
        Currency = string.Empty;
    }

    /// <summary>
    /// Creates a new draft payroll profile with a cached employee name.
    /// Used by <c>EmployeeOnboardedConsumer</c> when creating the profile from an integration event.
    /// </summary>
    public static PayrollProfile CreateDraft(Guid employeeId, string legalName)
    {
        return new PayrollProfile
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = legalName,
            PayType = PayType.Salaried,
            BaseSalary = 0.00m,
            HourlyRate = 0.00m,
            Currency = "USD",
            IsActive = false
        };
    }

    /// <summary>
    /// Creates a new draft payroll profile (for testing and simulation use).
    /// </summary>
    public static PayrollProfile CreateDraft(Guid employeeId)
    {
        return new PayrollProfile
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayType = PayType.Salaried,
            BaseSalary = 0.00m,
            HourlyRate = 0.00m,
            Currency = "USD",
            IsActive = false
        };
    }

    /// <summary>Sets the compensation as salaried.</summary>
    public void SetSalaried(decimal baseSalary)
    {
        PayType = PayType.Salaried;
        BaseSalary = baseSalary;
        HourlyRate = 0;
    }

    /// <summary>Sets the compensation as hourly.</summary>
    public void SetHourly(decimal hourlyRate)
    {
        PayType = PayType.Hourly;
        HourlyRate = hourlyRate;
        BaseSalary = 0;
    }

    /// <summary>Activates the profile.</summary>
    public void Activate() => IsActive = true;
}
