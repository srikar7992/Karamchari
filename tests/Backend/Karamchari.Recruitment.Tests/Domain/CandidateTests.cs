// -----------------------------------------------------------------------
// <copyright file="CandidateTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public class CandidateTests
{
    [Fact]
    public void CreateShouldInitializeWithCorrectValues()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@example.com";

        // Act
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create(firstName, lastName, email);

        // Assert
        candidate.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.CandidateId));
        candidate.FirstName.Should().Be(firstName);
        candidate.LastName.Should().Be(lastName);
        candidate.Email.Should().Be(email);
        candidate.ProfileVersion.Should().Be(1);
    }

    [Fact]
    public void UpdateProfileShouldUpdateAllProperties()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        var newFirst = "Jane";
        var newLast = "Smith";
        var newEmail = "jane@example.com";
        var newPhone = "1234567890";

        // Act
        candidate.UpdateProfile(newFirst, newLast, newEmail, newPhone);

        // Assert
        candidate.FirstName.Should().Be(newFirst);
        candidate.LastName.Should().Be(newLast);
        candidate.Email.Should().Be(newEmail);
        candidate.PhoneNumber.Should().Be(newPhone);
    }

    [Fact]
    public void UpdateProfileShouldIncrementVersion()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");

        // Act
        candidate.UpdateProfile("Jane", "Doe", "jane@example.com", null);

        // Assert
        candidate.ProfileVersion.Should().Be(2);

        // Act again
        candidate.UpdateProfile("Jane", "Smith", "jane@example.com", null);

        // Assert again
        candidate.ProfileVersion.Should().Be(3);
    }

    [Fact]
    public void CreateSnapshotShouldReturnMatchCurrentState()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        candidate.UpdateProfile("John", "Doe", "john@example.com", "555-1234");

        // Act
        var snapshot = candidate.CreateSnapshot();

        // Assert
        snapshot.FirstName.Should().Be(candidate.FirstName);
        snapshot.LastName.Should().Be(candidate.LastName);
        snapshot.Email.Should().Be(candidate.Email);
        snapshot.PhoneNumber.Should().Be(candidate.PhoneNumber);
        snapshot.Version.Should().Be(candidate.ProfileVersion);
    }

    [Fact]
    public void IdentityConsistencyVerifyIdAssignment()
    {
        // Act
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");

        // Assert
        candidate.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void CandidateIdShouldBeUnique()
    {
        // Act
        var c1 = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        var c2 = Karamchari.Recruitment.Domain.Candidate.Create("Jane", "Doe", "jane@example.com");

        // Assert
        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void CandidateIdShouldBeImmutable()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        var originalId = candidate.Id;

        // Act
        candidate.UpdateProfile("Jane", "Smith", "jane@example.com", "123");

        // Assert
        candidate.Id.Should().Be(originalId);
    }

    [Fact]
    public void UpdateProfileWithNullPhoneShouldSucceed()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        candidate.UpdateProfile("John", "Doe", "john@example.com", "123");

        // Act
        candidate.UpdateProfile("John", "Doe", "john@example.com", null);

        // Assert
        candidate.PhoneNumber.Should().BeNull();
        candidate.ProfileVersion.Should().Be(3);
    }

    [Fact]
    public void SnapshotShouldBeDeepCopyRecord()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        var snapshot1 = candidate.CreateSnapshot();

        // Act
        candidate.UpdateProfile("Jane", "Smith", "jane@example.com", "999");
        var snapshot2 = candidate.CreateSnapshot();

        // Assert
        snapshot1.FirstName.Should().Be("John");
        snapshot2.FirstName.Should().Be("Jane");
        snapshot1.Version.Should().Be(1);
        snapshot2.Version.Should().Be(2);
    }

    [Fact]
    public void SnapshotEqualityVerifyRecordBehavior()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        var snapshot1 = candidate.CreateSnapshot();
        var snapshot2 = new Karamchari.Recruitment.Domain.CandidateSnapshot(candidate.FirstName, candidate.LastName, candidate.Email, candidate.PhoneNumber, candidate.ProfileVersion);

        // Assert
        snapshot1.Should().Be(snapshot2);
    }

    [Fact]
    public void UpdateProfileMultipleCallsShouldKeepVersionConsistent()
    {
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        for (int i = 0; i < 10; i++)
        {
            candidate.UpdateProfile("John", "Doe", "john@example.com", i.ToString());
        }

        candidate.ProfileVersion.Should().Be(11);
    }

    [Fact]
    public void CreateWithEmptyStringsShouldBeAllowedByDomain()
    {
        // Domain doesn't currently validate against empty strings in constructor
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("", "", "");
        candidate.FirstName.Should().Be("");
    }

    [Fact]
    public void IdentityShouldBeSetOnCreation()
    {
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        candidate.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.CandidateId));
    }

    [Fact]
    public void PropertiesShouldBePrivateSet()
    {
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        // This is a compile-time check mostly, but ensures we use methods
        candidate.GetType().GetProperty(nameof(Karamchari.Recruitment.Domain.Candidate.FirstName))?.SetMethod?.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void UpdatedOnUtcShouldBeNullInitially()
    {
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john@example.com");
        candidate.UpdatedOnUtc.Should().BeNull();
    }
}
