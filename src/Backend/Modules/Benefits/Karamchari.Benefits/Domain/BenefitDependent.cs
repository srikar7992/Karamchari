// -----------------------------------------------------------------------
// <copyright file="BenefitDependent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Core.Domain.Primitives;

/// <summary>
/// A family member covered under the employee's benefit elections.
/// Owned by <see cref="BenefitEnrollment"/>; can be covered under Medical, Dental, and/or Vision.
/// IRS rules require date of birth for age-out tracking (children typically age out at 26).
/// </summary>
public sealed class BenefitDependent : Entity<BenefitDependentId>
{
    private BenefitDependent() { }

    internal BenefitDependent(
        BenefitDependentId id,
        BenefitEnrollmentId enrollmentId,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DependentRelationship relationship) : base(id)
    {
        EnrollmentId = enrollmentId;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Relationship = relationship;
        AddedAtUtc = DateTimeOffset.UtcNow;
    }

    public BenefitEnrollmentId EnrollmentId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public DependentRelationship Relationship { get; private set; }

    /// <summary>Whether this dependent is enrolled in the Medical plan.</summary>
    public bool IsEnrolledMedical { get; private set; }

    /// <summary>Whether this dependent is enrolled in the Dental plan.</summary>
    public bool IsEnrolledDental { get; private set; }

    /// <summary>Whether this dependent is enrolled in the Vision plan.</summary>
    public bool IsEnrolledVision { get; private set; }

    public DateTimeOffset AddedAtUtc { get; private set; }
    public bool IsRemoved { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }

    /// <summary>Age of the dependent as of a reference date (used for age-out rule evaluation).</summary>
    public int AgeOn(DateOnly referenceDate)
    {
        var age = referenceDate.Year - DateOfBirth.Year;
        if (DateOfBirth.AddYears(age) > referenceDate) age--;
        return age;
    }

    internal void UpdateCoverages(bool medical, bool dental, bool vision)
    {
        IsEnrolledMedical = medical;
        IsEnrolledDental = dental;
        IsEnrolledVision = vision;
    }

    internal void Remove()
    {
        IsRemoved = true;
        RemovedAtUtc = DateTimeOffset.UtcNow;
    }
}
