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
/// </summary>
public class PayrollProfile : ITenantOwned
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier for the profile.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the employee identifier (Soft reference to HR Domain).
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the cached employee name for reporting and payslips.
    /// </summary>
    public string EmployeeName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the compensation type.
    /// </summary>
    public PayType PayType { get; private set; }

    /// <summary>
    /// Gets the fixed monthly base salary. Only used if PayType is Salaried.
    /// </summary>
    public decimal BaseSalary { get; private set; }

    /// <summary>
    /// Gets the hourly rate. Only used if PayType is Hourly.
    /// </summary>
    public decimal HourlyRate { get; private set; }

    /// <summary>
    /// Gets the currency code (e.g., USD).
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the profile is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the employee has opted for Voluntary PF (overriding the â‚¹15,000 cap).
    /// </summary>
    public bool OptedForVoluntaryPF { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ESIC is locked for the current contribution period.
    /// </summary>
    public bool IsEsicLocked { get; private set; }

    /// <summary>
    /// Gets the state code for Professional Tax jurisdiction (e.g., "TS").
    /// </summary>
    public string StateCode { get; private set; } = "TS";

    /// <summary>
    /// Gets the income tax regime selected by the employee.
    /// </summary>
    public TaxRegime TaxRegime { get; private set; } = TaxRegime.New;

    /// <summary>
    /// Gets a value indicating whether the employee's work location is in a Metro city (for HRA).
    /// </summary>
    public bool IsMetro { get; private set; }

    /// <summary>Gets the Total Annual CTC.</summary>
    public decimal AnnualCTC { get; private set; }

    /// <summary>Gets the assigned Salary Template ID.</summary>
    public Guid SalaryTemplateId { get; private set; }

    /// <summary>Gets the Permanent Account Number (Income Tax).</summary>
    public string Pan { get; private set; } = string.Empty;

    /// <summary>Gets the Universal Account Number (PF).</summary>
    public string Uan { get; private set; } = string.Empty;

    /// <summary>Gets the ESIC Insurance Number.</summary>
    public string EsicNumber { get; private set; } = string.Empty;

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
    /// Updates the statutory identifiers for the profile.
    /// </summary>
    public void UpdateStatutoryInfo(string pan, string uan, string esicNumber)
    {
        Pan = pan;
        Uan = uan;
        EsicNumber = esicNumber;
    }

    /// <summary>
    /// Creates a shallow clone with an overridden tax regime (for simulations).
    /// </summary>
    public PayrollProfile CloneWithRegime(TaxRegime regime)
    {
        return new PayrollProfile
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
            OptedForVoluntaryPF = this.OptedForVoluntaryPF,
            IsEsicLocked = this.IsEsicLocked,
            StateCode = this.StateCode,
            IsMetro = this.IsMetro,
            AnnualCTC = this.AnnualCTC,
            SalaryTemplateId = this.SalaryTemplateId,
            TaxRegime = regime
        };
    }

    private PayrollProfile()
    {
        Currency = string.Empty;
    }

    /// <summary>
    /// Creates a new draft payroll profile with a cached employee name.
    /// Used by <c>EmployeeOnboardedConsumer</c> when creating the profile from an integration event.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="legalName">The employee's legal name cached for reporting and payslips.</param>
    /// <returns>A new <see cref="PayrollProfile"/> instance.</returns>
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
    /// <param name="employeeId">The employee identifier.</param>
    /// <returns>A new <see cref="PayrollProfile"/> instance.</returns>
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

    /// <summary>
    /// Sets the compensation as salaried.
    /// </summary>
    /// <param name="baseSalary">The base salary.</param>
    public void SetSalaried(decimal baseSalary)
    {
        PayType = PayType.Salaried;
        BaseSalary = baseSalary;
        HourlyRate = 0;
    }

    /// <summary>
    /// Sets the compensation as hourly.
    /// </summary>
    /// <param name="hourlyRate">The hourly rate.</param>
    public void SetHourly(decimal hourlyRate)
    {
        PayType = PayType.Hourly;
        HourlyRate = hourlyRate;
        BaseSalary = 0;
    }

    /// <summary>
    /// Activates the profile.
    /// </summary>
    public void Activate() => IsActive = true;
}
