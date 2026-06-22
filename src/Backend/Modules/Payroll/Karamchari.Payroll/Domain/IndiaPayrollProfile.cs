// -----------------------------------------------------------------------
// <copyright file="IndiaPayrollProfile.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain;

using Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// India-specific statutory configuration for a payroll profile.
/// Exists only for employees subject to Indian labour law. A UAE employee's
/// <see cref="PayrollProfile"/> will have <c>India == null</c>.
///
/// EF Core maps this as a separate table (<c>IndiaPayrollProfiles</c>) with a 1:1
/// relationship to <see cref="PayrollProfile"/>; <c>PayrollProfileId</c> is both
/// the primary key candidate and the FK, enforcing the 1:1 at the database level.
/// </summary>
public sealed class IndiaPayrollProfile
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the owning <see cref="PayrollProfile"/> identifier.</summary>
    public Guid PayrollProfileId { get; private set; }

    /// <summary>Gets a value indicating whether the employee has opted for Voluntary PF (overriding the ₹15,000 cap).</summary>
    public bool OptedForVoluntaryPF { get; private set; }

    /// <summary>Gets a value indicating whether ESIC is locked for the current contribution period.</summary>
    public bool IsEsicLocked { get; private set; }

    /// <summary>Gets the state code for Professional Tax jurisdiction (e.g., "TS" for Telangana).</summary>
    public string StateCode { get; private set; } = "TS";

    /// <summary>Gets the income tax regime selected by the employee.</summary>
    public TaxRegime TaxRegime { get; private set; } = TaxRegime.New;

    /// <summary>Gets a value indicating whether the employee's work location is in a Metro city (for HRA).</summary>
    public bool IsMetro { get; private set; }

    /// <summary>Gets the Permanent Account Number (Income Tax).</summary>
    public string Pan { get; private set; } = string.Empty;

    /// <summary>Gets the Universal Account Number (PF).</summary>
    public string Uan { get; private set; } = string.Empty;

    /// <summary>Gets the ESIC Insurance Number.</summary>
    public string EsicNumber { get; private set; } = string.Empty;

    private IndiaPayrollProfile() { }

    /// <summary>
    /// Creates a new India statutory configuration attached to an existing payroll profile.
    /// Pan, Uan, and EsicNumber are set separately via <see cref="UpdateIdentifiers"/> once HR captures them.
    /// </summary>
    public static IndiaPayrollProfile Create(
        Guid payrollProfileId,
        string stateCode = "TS",
        TaxRegime taxRegime = TaxRegime.New,
        bool isMetro = false,
        bool optedForVoluntaryPF = false,
        bool isEsicLocked = false) => new()
        {
            Id = Guid.NewGuid(),
            PayrollProfileId = payrollProfileId,
            StateCode = stateCode,
            TaxRegime = taxRegime,
            IsMetro = isMetro,
            OptedForVoluntaryPF = optedForVoluntaryPF,
            IsEsicLocked = isEsicLocked
        };

    /// <summary>
    /// Updates the statutory identifiers (PAN, UAN, ESIC Number).
    /// Called when HR enters the employee's government-issued IDs.
    /// </summary>
    public void UpdateIdentifiers(string pan, string uan, string esicNumber)
    {
        Pan = pan;
        Uan = uan;
        EsicNumber = esicNumber;
    }

    /// <summary>
    /// Creates a shallow clone with an overridden tax regime (for simulations).
    /// All identifiers are copied so simulation compliance reports remain valid.
    /// </summary>
    public IndiaPayrollProfile CloneWithRegime(TaxRegime regime) => new()
    {
        Id = Guid.NewGuid(),
        PayrollProfileId = PayrollProfileId,
        StateCode = StateCode,
        TaxRegime = regime,
        IsMetro = IsMetro,
        OptedForVoluntaryPF = OptedForVoluntaryPF,
        IsEsicLocked = IsEsicLocked,
        Pan = Pan,
        Uan = Uan,
        EsicNumber = EsicNumber
    };
}
