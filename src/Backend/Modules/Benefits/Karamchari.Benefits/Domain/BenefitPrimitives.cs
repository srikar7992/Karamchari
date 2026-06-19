// -----------------------------------------------------------------------
// <copyright file="BenefitPrimitives.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

// ── Strongly-typed identifiers ─────────────────────────────────────────────
// Using record structs so EF Core value conversions stay simple and there
// is no accidental Guid-to-Guid assignment across aggregate boundaries.

public record struct BenefitPlanId(Guid Value);
public record struct BenefitEnrollmentId(Guid Value);
public record struct BenefitElectionId(Guid Value);
public record struct BenefitDependentId(Guid Value);
public record struct LifeEventId(Guid Value);

// ── Plan-level enumerations ────────────────────────────────────────────────

/// <summary>
/// The type of benefit a plan covers. Used to group plans in the enrollment UI
/// and to ensure an employee elects at most one plan per category.
/// </summary>
public enum BenefitCategory
{
    Medical = 1,
    Dental = 2,
    Vision = 3,
    LifeInsurance = 4,
    ShortTermDisability = 5,
    LongTermDisability = 6,
    RetirementContribution = 7,
    FlexibleSpendingAccount = 8,
    HealthSavingsAccount = 9,
    SupplementalLife = 10
}

/// <summary>
/// Coverage tier that determines who is covered and drives the premium cost.
/// The UI renders these labels to the employee during enrollment.
/// </summary>
public enum CoverageTier
{
    EmployeeOnly = 1,
    EmployeePlusSpouse = 2,
    EmployeePlusChildren = 3,
    Family = 4
}

/// <summary>Lifecycle state of a benefit plan in the admin catalog.</summary>
public enum BenefitPlanStatus
{
    /// <summary>Created but not yet made available for enrollment.</summary>
    Draft = 0,

    /// <summary>Available for employee elections during the enrollment window.</summary>
    Active = 1,

    /// <summary>Temporarily suspended — existing enrollees keep coverage but no new elections.</summary>
    Inactive = 2,

    /// <summary>Permanently withdrawn — no new enrollments; existing enrollments run out their term.</summary>
    Discontinued = 3
}

// ── Enrollment-level enumerations ─────────────────────────────────────────

/// <summary>Lifecycle state of an employee's benefit enrollment for a plan year.</summary>
public enum EnrollmentStatus
{
    /// <summary>Employee has not yet submitted elections for this plan year.</summary>
    PendingSubmission = 0,

    /// <summary>Employee submitted elections; pending HR review or life-event verification.</summary>
    Submitted = 1,

    /// <summary>Elections are confirmed and payroll deductions are active.</summary>
    Active = 2,

    /// <summary>Coverage has ended (year closed, termination, or waiver applied mid-year).</summary>
    Terminated = 3,

    /// <summary>Employee explicitly opted out of all benefits for this plan year.</summary>
    WaivedAllCoverage = 4
}

// ── Life event enumerations ────────────────────────────────────────────────

/// <summary>
/// Qualifying life events that open a special enrollment window outside open enrollment.
/// Each type has a standard IRS-defined window (typically 30 days from the event date).
/// </summary>
public enum LifeEventType
{
    Marriage = 1,
    Divorce = 2,
    NewChild = 3,
    AdoptedChild = 4,
    LossOfOtherCoverage = 5,
    DependentAgeOut = 6,
    SpouseEmploymentChange = 7,
    Death = 8,
    NewHire = 9
}

/// <summary>Verification status of a submitted life event.</summary>
public enum LifeEventStatus
{
    /// <summary>Submitted by employee, awaiting HR/admin verification of supporting document.</summary>
    PendingVerification = 0,

    /// <summary>Document verified; election window is open.</summary>
    Verified = 1,

    /// <summary>Document rejected; employee must resubmit or the window closes without change.</summary>
    Rejected = 2,

    /// <summary>The qualifying event was verified and the employee completed their election changes.</summary>
    Completed = 3
}

// ── Dependent enumerations ─────────────────────────────────────────────────

/// <summary>Relationship between the dependent and the enrolled employee.</summary>
public enum DependentRelationship
{
    Spouse = 1,
    DomesticPartner = 2,
    Child = 3,
    StepChild = 4,
    AdoptedChild = 5,
    FosterChild = 6
}
