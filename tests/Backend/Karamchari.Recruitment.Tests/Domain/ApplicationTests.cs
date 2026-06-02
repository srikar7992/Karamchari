using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public class ApplicationTests
{
    private readonly Karamchari.Recruitment.Domain.CandidateId candidateId = Karamchari.Recruitment.Domain.CandidateId.New();
    private readonly Karamchari.Recruitment.Domain.RequisitionId requisitionId = Karamchari.Recruitment.Domain.RequisitionId.New();
    private readonly Karamchari.Recruitment.Domain.CandidateSnapshot snapshot = new("John", "Doe", "john@example.com", "123", 1);

    [Fact]
    public void CreateShouldInitializeWithCorrectValues()
    {
        // Act
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        // Assert
        application.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.ApplicationId));
        application.CandidateId.Should().Be(candidateId);
        application.RequisitionId.Should().Be(requisitionId);
        application.CandidateSnapshot.Should().Be(snapshot);
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.New);
        application.AppliedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreateShouldRaiseCandidateAppliedDomainEvent()
    {
        // Act
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        // Assert
        application.DomainEvents.Should().ContainSingle(e => e is CandidateAppliedDomainEvent);
    }

    [Fact]
    public void AdvanceToScreeningWhenNewShouldSucceed()
    {
        // Arrange
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        // Act
        application.AdvanceToScreening();

        // Assert
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Screening);
        application.DomainEvents.Should().Contain(e => e is ApplicationAdvancedDomainEvent);
    }

    [Theory]
    [InlineData(Karamchari.Recruitment.Domain.ApplicationStatus.Screening)]
    [InlineData(Karamchari.Recruitment.Domain.ApplicationStatus.Interviewing)]
    [InlineData(Karamchari.Recruitment.Domain.ApplicationStatus.Offered)]
    [InlineData(Karamchari.Recruitment.Domain.ApplicationStatus.Hired)]
    [InlineData(Karamchari.Recruitment.Domain.ApplicationStatus.Rejected)]
    public void AdvanceToScreeningWhenNotNewShouldThrow(Karamchari.Recruitment.Domain.ApplicationStatus currentStatus)
    {
        // Arrange
        var application = CreateInStatus(currentStatus);

        // Act
        Action act = () => application.AdvanceToScreening();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvanceToInterviewingWhenScreeningShouldSucceed()
    {
        // Arrange
        var application = CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus.Screening);

        // Act
        application.AdvanceToInterviewing();

        // Assert
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Interviewing);
    }

    [Fact]
    public void MarkAsOfferedWhenInterviewingShouldSucceed()
    {
        // Arrange
        var application = CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus.Interviewing);

        // Act
        application.MarkAsOffered();

        // Assert
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Offered);
    }

    [Fact]
    public void HireWhenOfferedShouldSucceed()
    {
        // Arrange
        var application = CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus.Offered);
        var hiredBy = "Manager";

        // Act
        application.Hire(hiredBy);

        // Assert
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Hired);
        application.HiredBy.Should().Be(hiredBy);
        application.HiredAt.Should().NotBeNull();
        application.DomainEvents.Should().Contain(e => e is CandidateHiredDomainEvent);
    }

    [Fact]
    public void RejectFromAnyStateShouldSucceed()
    {
        // Arrange
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        // Act
        application.Reject();

        // Assert
        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Rejected);
    }

    [Fact]
    public void SnapshotIntegrityVerifyDataMatchesSnapshot()
    {
        // Arrange
        var customSnapshot = new Karamchari.Recruitment.Domain.CandidateSnapshot("Jane", "Smith", "jane@example.com", "999", 5);

        // Act
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, customSnapshot);

        // Assert
        application.CandidateSnapshot.FirstName.Should().Be("Jane");
        application.CandidateSnapshot.LastName.Should().Be("Smith");
        application.CandidateSnapshot.Version.Should().Be(5);
    }

    [Fact]
    public void IdentityConsistencyVerifyIdAssignment()
    {
        // Act
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        // Assert
        application.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplicationIdShouldBeUnique()
    {
        var a1 = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);
        var a2 = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);
        a1.Id.Should().NotBe(a2.Id);
    }

    [Fact]
    public void ApplicationIdShouldBeImmutable()
    {
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);
        var id = application.Id;
        application.AdvanceToScreening();
        application.Reject();
        application.Id.Should().Be(id);
    }

    [Fact]
    public void HireWhenRejectedShouldThrow()
    {
        var application = CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus.Rejected);
        Action act = () => application.Hire("Manager");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MultipleAdvancesShouldFollowHappyPath()
    {
        var application = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);

        application.AdvanceToScreening();
        application.AdvanceToInterviewing();
        application.MarkAsOffered();
        application.Hire("Admin");

        application.Status.Should().Be(Karamchari.Recruitment.Domain.ApplicationStatus.Hired);
    }

    [Fact]
    public void RejectShouldThrowWhenApplicationIsAlreadyHired()
    {
        // Hired is a terminal state — rejecting a hired application is a business invariant violation.
        var application = CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus.Hired);

        var act = () => application.Reject();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*terminal state*");
    }

    private Karamchari.Recruitment.Domain.Application CreateInStatus(Karamchari.Recruitment.Domain.ApplicationStatus status)
    {
        var app = Karamchari.Recruitment.Domain.Application.Create(candidateId, requisitionId, snapshot);
        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.New) return app;

        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.Rejected)
        {
            app.Reject();
            return app;
        }

        app.AdvanceToScreening();
        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.Screening) return app;

        app.AdvanceToInterviewing();
        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.Interviewing) return app;

        app.MarkAsOffered();
        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.Offered) return app;

        if (status == Karamchari.Recruitment.Domain.ApplicationStatus.Hired)
        {
            app.Hire("System");
            return app;
        }

        return app;
    }
}
