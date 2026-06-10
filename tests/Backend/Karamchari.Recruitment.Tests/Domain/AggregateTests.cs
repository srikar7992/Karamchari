// -----------------------------------------------------------------------
// <copyright file="AggregateTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public sealed class AggregateTests
{
    [Fact]
    public void RequisitionShouldFollowStateMachine()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Software Engineer", Guid.NewGuid(), Guid.NewGuid());
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Draft);

        // Act
        requisition.SubmitForApproval();
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.PendingApproval);

        requisition.Approve();
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Published);

        // Assert
        requisition.DomainEvents.Should().Contain(e => e is RequisitionCreatedDomainEvent);
        requisition.DomainEvents.Should().Contain(e => e is RequisitionPublishedDomainEvent);
    }

    [Fact]
    public void CandidateShouldCreateSnapshotCorrectly()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john.doe@example.com");
        candidate.UpdateProfile("John", "Smith", "john.smith@example.com", "12345");

        // Act
        var snapshot = candidate.CreateSnapshot();

        // Assert
        snapshot.FirstName.Should().Be("John");
        snapshot.LastName.Should().Be("Smith");
        snapshot.Version.Should().Be(2);
    }

    [Fact]
    public void ApplicationShouldTransitionCorrectly()
    {
        // Arrange
        var candidate = Karamchari.Recruitment.Domain.Candidate.Create("John", "Doe", "john.doe@example.com");
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Software Engineer", Guid.NewGuid(), Guid.NewGuid());
        var snapshot = candidate.CreateSnapshot();

        var application = Karamchari.Recruitment.Domain.Application.Create(candidate.Id, requisition.Id, snapshot);
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.New);

        // Act
        application.AdvanceToScreening();
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Screening);

        application.AdvanceToInterviewing();
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Interviewing);

        application.MarkAsOffered();
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Offered);

        application.Hire("recruiter-user");
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Hired);

        // Assert
        application.DomainEvents.Should().Contain(e => e is CandidateAppliedDomainEvent);
        application.DomainEvents.Should().Contain(e => e is CandidateHiredDomainEvent);
    }

    [Fact]
    public void OfferShouldEnforceExpiration()
    {
        // Arrange
        var offer = Karamchari.Recruitment.Domain.Offer.Create(Karamchari.Recruitment.Domain.ApplicationId.New(), 100000, "USD");
        offer.SubmitForApproval();
        offer.Approve();

        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        offer.Issue(pastDate);

        // Act
        var act = () => offer.Accept();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Offer has expired.");
    }
}
