using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public class InterviewTests
{
    private readonly Karamchari.Recruitment.Domain.ApplicationId applicationId = Karamchari.Recruitment.Domain.ApplicationId.New();
    private readonly DateTimeOffset scheduledAt = DateTimeOffset.UtcNow.AddDays(1);
    private readonly int durationMinutes = 60;

    [Fact]
    public void CreateShouldInitializeWithCorrectValues()
    {
        // Act
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Assert
        interview.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.InterviewId));
        interview.ApplicationId.Should().Be(applicationId);
        interview.ScheduledAt.Should().Be(scheduledAt);
        interview.DurationMinutes.Should().Be(durationMinutes);
        interview.Status.Should().Be(Karamchari.Recruitment.Domain.InterviewStatus.Scheduled);
        interview.InterviewerIds.Should().BeEmpty();
    }

    [Fact]
    public void CreateShouldRaiseInterviewScheduledDomainEvent()
    {
        // Act
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Assert
        interview.DomainEvents.Should().ContainSingle(e => e is InterviewScheduledDomainEvent);
    }

    [Fact]
    public void AddInterviewerShouldAddToList()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        var interviewerId = Guid.NewGuid();

        // Act
        interview.AddInterviewer(interviewerId);

        // Assert
        interview.InterviewerIds.Should().Contain(interviewerId);
        interview.InterviewerIds.Should().HaveCount(1);
    }

    [Fact]
    public void AddInterviewerShouldNotAddDuplicate()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        var interviewerId = Guid.NewGuid();

        // Act
        interview.AddInterviewer(interviewerId);
        interview.AddInterviewer(interviewerId);

        // Assert
        interview.InterviewerIds.Should().HaveCount(1);
    }

    [Fact]
    public void AddMultipleInterviewersShouldSucceed()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Act
        interview.AddInterviewer(Guid.NewGuid());
        interview.AddInterviewer(Guid.NewGuid());

        // Assert
        interview.InterviewerIds.Should().HaveCount(2);
    }

    [Fact]
    public void CompleteWhenScheduledShouldChangeStatusToCompleted()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Act
        interview.Complete();

        // Assert
        interview.Status.Should().Be(Karamchari.Recruitment.Domain.InterviewStatus.Completed);
    }

    [Fact]
    public void CompleteShouldRaiseInterviewCompletedDomainEvent()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Act
        interview.Complete();

        // Assert
        interview.DomainEvents.Should().Contain(e => e is InterviewCompletedDomainEvent);
    }

    [Theory]
    [InlineData(Karamchari.Recruitment.Domain.InterviewStatus.Completed)]
    [InlineData(Karamchari.Recruitment.Domain.InterviewStatus.Cancelled)]
    public void CompleteWhenNotScheduledShouldThrow(Karamchari.Recruitment.Domain.InterviewStatus status)
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        if (status == Karamchari.Recruitment.Domain.InterviewStatus.Completed) interview.Complete();
        else if (status == Karamchari.Recruitment.Domain.InterviewStatus.Cancelled) interview.Cancel();

        // Act
        Action act = () => interview.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancelShouldSetStatusToCancelled()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);

        // Act
        interview.Cancel();

        // Assert
        interview.Status.Should().Be(Karamchari.Recruitment.Domain.InterviewStatus.Cancelled);
    }

    [Fact]
    public void CancelFromCompletedStateShouldSucceed()
    {
        // Arrange
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        interview.Complete();

        // Act
        interview.Cancel();

        // Assert
        interview.Status.Should().Be(Karamchari.Recruitment.Domain.InterviewStatus.Cancelled);
    }

    [Fact]
    public void InterviewIdShouldBeUnique()
    {
        var i1 = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        var i2 = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        i1.Id.Should().NotBe(i2.Id);
    }

    [Fact]
    public void IdentityConsistencyVerifyIdAssignment()
    {
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        interview.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void IdentityShouldBeImmutable()
    {
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, durationMinutes);
        var id = interview.Id;
        interview.AddInterviewer(Guid.NewGuid());
        interview.Complete();
        interview.Id.Should().Be(id);
    }

    [Fact]
    public void ScheduleInPastCurrentImplementationAllowsIt()
    {
        // Testing current boundary behavior
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, pastDate, 60);
        interview.ScheduledAt.Should().Be(pastDate);
    }

    [Fact]
    public void DurationZeroCurrentImplementationAllowsIt()
    {
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, 0);
        interview.DurationMinutes.Should().Be(0);
    }

    [Fact]
    public void CancelWhenAlreadyCancelledShouldStillBeCancelled()
    {
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, 60);
        interview.Cancel();
        interview.Cancel();
        interview.Status.Should().Be(Karamchari.Recruitment.Domain.InterviewStatus.Cancelled);
    }

    [Fact]
    public void MultipleInterviewerAdditionsShouldMaintainSetBehavior()
    {
        var interview = Karamchari.Recruitment.Domain.Interview.Create(applicationId, scheduledAt, 60);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        interview.AddInterviewer(id1);
        interview.AddInterviewer(id2);
        interview.AddInterviewer(id1);

        interview.InterviewerIds.Should().HaveCount(2);
        interview.InterviewerIds.Should().Contain(new[] { id1, id2 });
    }
}
