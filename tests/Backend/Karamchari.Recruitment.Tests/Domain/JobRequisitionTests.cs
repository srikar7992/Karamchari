using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public class JobRequisitionTests
{
    [Fact]
    public void CreateShouldInitializeWithCorrectValues()
    {
        // Arrange
        var title = "Software Engineer";
        var departmentId = Guid.NewGuid();
        var hiringManagerId = Guid.NewGuid();

        // Act
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create(title, departmentId, hiringManagerId);

        // Assert
        requisition.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.RequisitionId));
        requisition.Title.Should().Be(title);
        requisition.DepartmentId.Should().Be(departmentId);
        requisition.HiringManagerId.Should().Be(hiringManagerId);
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Draft);
    }

    [Fact]
    public void CreateShouldRaiseRequisitionCreatedDomainEvent()
    {
        // Act
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());

        // Assert
        requisition.DomainEvents.Should().ContainSingle(e => e is RequisitionCreatedDomainEvent);
    }

    [Fact]
    public void SubmitForApprovalWhenDraftShouldChangeStatusToPendingApproval()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());

        // Act
        requisition.SubmitForApproval();

        // Assert
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.PendingApproval);
    }

    [Theory]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.PendingApproval)]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.Published)]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.Closed)]
    public void SubmitForApprovalWhenNotDraftShouldThrowInvalidOperationException(Karamchari.Recruitment.Domain.RequisitionStatus status)
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        if (status == Karamchari.Recruitment.Domain.RequisitionStatus.PendingApproval) requisition.SubmitForApproval();
        else if (status == Karamchari.Recruitment.Domain.RequisitionStatus.Published) { requisition.SubmitForApproval(); requisition.Approve(); }
        else if (status == Karamchari.Recruitment.Domain.RequisitionStatus.Closed) requisition.Close();

        // Act
        Action act = () => requisition.SubmitForApproval();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApproveWhenPendingApprovalShouldChangeStatusToPublished()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        requisition.SubmitForApproval();

        // Act
        requisition.Approve();

        // Assert
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Published);
    }

    [Fact]
    public void ApproveShouldRaiseRequisitionPublishedDomainEvent()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        requisition.SubmitForApproval();
        requisition.ClearDomainEvents();

        // Act
        requisition.Approve();

        // Assert
        requisition.DomainEvents.Should().ContainSingle(e => e is RequisitionPublishedDomainEvent);
    }

    [Theory]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.Draft)]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.Published)]
    [InlineData(Karamchari.Recruitment.Domain.RequisitionStatus.Closed)]
    public void ApproveWhenNotPendingApprovalShouldThrowInvalidOperationException(Karamchari.Recruitment.Domain.RequisitionStatus status)
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        if (status == Karamchari.Recruitment.Domain.RequisitionStatus.Published) { requisition.SubmitForApproval(); requisition.Approve(); }
        else if (status == Karamchari.Recruitment.Domain.RequisitionStatus.Closed) requisition.Close();

        // Act
        Action act = () => requisition.Approve();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CloseShouldAlwaysChangeStatusToClosed()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());

        // Act
        requisition.Close();

        // Assert
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Closed);
    }

    [Fact]
    public void CloseFromPublishedStateShouldSucceed()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        requisition.SubmitForApproval();
        requisition.Approve();

        // Act
        requisition.Close();

        // Assert
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Closed);
    }

    [Fact]
    public void RequisitionIdShouldBeUnique()
    {
        // Act
        var req1 = Karamchari.Recruitment.Domain.JobRequisition.Create("T1", Guid.NewGuid(), Guid.NewGuid());
        var req2 = Karamchari.Recruitment.Domain.JobRequisition.Create("T2", Guid.NewGuid(), Guid.NewGuid());

        // Assert
        req1.Id.Should().NotBe(req2.Id);
    }

    [Fact]
    public void RequisitionIdShouldBeImmutable()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        var originalId = requisition.Id;

        // Act
        requisition.SubmitForApproval();
        requisition.Approve();
        requisition.Close();

        // Assert
        requisition.Id.Should().Be(originalId);
    }

    [Fact]
    public void IdentityConsistencyVerifyIdAssignment()
    {
        // Arrange
        var title = "Title";
        var depId = Guid.NewGuid();
        var hmId = Guid.NewGuid();

        // Act
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create(title, depId, hmId);

        // Assert
        requisition.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void MultipleTransitionsShouldFollowHappyPath()
    {
        // Arrange
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Draft);
        
        requisition.SubmitForApproval();
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.PendingApproval);
        
        requisition.Approve();
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Published);
        
        requisition.Close();
        requisition.Status.Should().Be(Karamchari.Recruitment.Domain.RequisitionStatus.Closed);
    }

    [Fact]
    public void SubmitForApprovalWhenAlreadyPendingShouldThrow()
    {
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        requisition.SubmitForApproval();

        Action act = () => requisition.SubmitForApproval();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApproveWhenAlreadyPublishedShouldThrow()
    {
        var requisition = Karamchari.Recruitment.Domain.JobRequisition.Create("Title", Guid.NewGuid(), Guid.NewGuid());
        requisition.SubmitForApproval();
        requisition.Approve();

        Action act = () => requisition.Approve();
        act.Should().Throw<InvalidOperationException>();
    }
}
