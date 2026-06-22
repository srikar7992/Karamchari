// -----------------------------------------------------------------------
// <copyright file="BenefitElection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// An employee's coverage choice within a single benefit category for a plan year.
/// Owned by <see cref="BenefitEnrollment"/>; at most one active election per category.
///
/// Costs are snapshotted from the plan at the time of election so that mid-year
/// plan rate changes do not retroactively alter the employee's committed deductions.
/// </summary>
public sealed class BenefitElection : Entity<BenefitElectionId>
{
    private BenefitElection() { }

    internal BenefitElection(
        BenefitElectionId id,
        BenefitEnrollmentId enrollmentId,
        BenefitPlanId planId,
        BenefitCategory category,
        CoverageTier coverageTier,
        decimal employeeMonthlyPremium,
        decimal employerMonthlyPremium,
        DateOnly effectiveDate) : base(id)
    {
        EnrollmentId = enrollmentId;
        PlanId = planId;
        Category = category;
        CoverageTier = coverageTier;
        EmployeeMonthlyPremium = employeeMonthlyPremium;
        EmployerMonthlyPremium = employerMonthlyPremium;
        EffectiveDate = effectiveDate;
        ElectedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public BenefitEnrollmentId EnrollmentId { get; private set; }

    /// <summary>The specific plan elected (snapshot reference; plan may be updated post-election).</summary>
    public BenefitPlanId PlanId { get; private set; }

    public BenefitCategory Category { get; private set; }
    public CoverageTier CoverageTier { get; private set; }

    /// <summary>Employee premium snapshotted at election time. Not updated when plan costs change.</summary>
    public decimal EmployeeMonthlyPremium { get; private set; }

    /// <summary>Employer premium snapshotted at election time.</summary>
    public decimal EmployerMonthlyPremium { get; private set; }

    public decimal TotalMonthlyPremium => EmployeeMonthlyPremium + EmployerMonthlyPremium;

    /// <summary>Date from which payroll deductions for this election begin.</summary>
    public DateOnly EffectiveDate { get; private set; }

    public DateTimeOffset ElectedAtUtc { get; private set; }

    /// <summary>
    /// False when this election has been superseded by a life-event change.
    /// Superseded elections are retained for audit — never deleted.
    /// </summary>
    public bool IsActive { get; private set; }
    public DateTimeOffset? SupersededAtUtc { get; private set; }

    internal void Supersede()
    {
        IsActive = false;
        SupersededAtUtc = DateTimeOffset.UtcNow;
    }
}
