using FluentAssertions;
using Karamchari.Recruitment.Domain;
using Xunit;

namespace Karamchari.Recruitment.Tests.Domain;

public class OfferTests
{
    private readonly Karamchari.Recruitment.Domain.ApplicationId applicationId = Karamchari.Recruitment.Domain.ApplicationId.New();
    private readonly decimal baseSalary = 100000;
    private readonly string currency = "USD";

    [Fact]
    public void CreateShouldInitializeWithCorrectValues()
    {
        // Act
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);

        // Assert
        offer.Id.Should().NotBe(default(Karamchari.Recruitment.Domain.OfferId));
        offer.ApplicationId.Should().Be(applicationId);
        offer.BaseSalary.Should().Be(baseSalary);
        offer.Currency.Should().Be(currency);
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Draft);
    }

    [Fact]
    public void CreateShouldRaiseOfferDraftedDomainEvent()
    {
        // Act
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);

        // Assert
        offer.DomainEvents.Should().ContainSingle(e => e is OfferDraftedDomainEvent);
    }

    [Fact]
    public void SubmitForApprovalWhenDraftShouldChangeStatus()
    {
        // Arrange
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);

        // Act
        offer.SubmitForApproval();

        // Assert
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.PendingApproval);
    }

    [Theory]
    [InlineData(Karamchari.Recruitment.Domain.OfferStatus.PendingApproval)]
    [InlineData(Karamchari.Recruitment.Domain.OfferStatus.Approved)]
    [InlineData(Karamchari.Recruitment.Domain.OfferStatus.Issued)]
    public void SubmitForApprovalWhenNotDraftShouldThrow(Karamchari.Recruitment.Domain.OfferStatus status)
    {
        // Arrange
        var offer = CreateInStatus(status);

        // Act
        Action act = () => offer.SubmitForApproval();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApproveWhenPendingApprovalShouldChangeStatus()
    {
        // Arrange
        var offer = CreateInStatus(Karamchari.Recruitment.Domain.OfferStatus.PendingApproval);

        // Act
        offer.Approve();

        // Assert
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Approved);
        offer.DomainEvents.Should().Contain(e => e is OfferApprovedDomainEvent);
    }

    [Fact]
    public void IssueWhenApprovedShouldSetIssuedAtAndExpiresAt()
    {
        // Arrange
        var offer = CreateInStatus(Karamchari.Recruitment.Domain.OfferStatus.Approved);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        offer.Issue(expiresAt);

        // Assert
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Issued);
        offer.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        offer.ExpiresAt.Should().Be(expiresAt);
        offer.DomainEvents.Should().Contain(e => e is OfferIssuedDomainEvent);
    }

    [Fact]
    public void AcceptWhenIssuedAndNotExpiredShouldSucceed()
    {
        // Arrange
        var offer = CreateInStatus(Karamchari.Recruitment.Domain.OfferStatus.Issued);

        // Act
        offer.Accept();

        // Assert
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Accepted);
        offer.DomainEvents.Should().Contain(e => e is OfferAcceptedDomainEvent);
    }

    [Fact]
    public void AcceptWhenExpiredShouldThrow()
    {
        // Arrange
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        offer.SubmitForApproval();
        offer.Approve();

        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        offer.Issue(pastDate);

        // Act
        Action act = () => offer.Accept();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Offer has expired.");
    }

    [Fact]
    public void DeclineShouldChangeStatusToDeclined()
    {
        // Arrange
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);

        // Act
        offer.Decline();

        // Assert
        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Declined);
        offer.DomainEvents.Should().Contain(e => e is OfferDeclinedDomainEvent);
    }

    [Fact]
    public void OfferIdShouldBeUnique()
    {
        var o1 = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        var o2 = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        o1.Id.Should().NotBe(o2.Id);
    }

    [Fact]
    public void IdentityConsistencyVerifyIdAssignment()
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        offer.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void IdentityShouldBeImmutable()
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        var id = offer.Id;
        offer.SubmitForApproval();
        offer.Approve();
        offer.Id.Should().Be(id);
    }

    [Fact]
    public void BoundaryZeroSalaryCurrentImplementationAllowsIt()
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, 0, currency);
        offer.BaseSalary.Should().Be(0);
    }

    [Fact]
    public void MultipleTransitionsShouldFollowHappyPath()
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);

        offer.SubmitForApproval();
        offer.Approve();
        offer.Issue(DateTimeOffset.UtcNow.AddDays(7));
        offer.Accept();

        offer.Status.Should().Be(Karamchari.Recruitment.Domain.OfferStatus.Accepted);
    }

    [Fact]
    public void ApproveWhenDraftShouldThrow()
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        Action act = () => offer.Approve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IssueWhenPendingApprovalShouldThrow()
    {
        var offer = CreateInStatus(Karamchari.Recruitment.Domain.OfferStatus.PendingApproval);
        Action act = () => offer.Issue(DateTimeOffset.UtcNow.AddDays(7));
        act.Should().Throw<InvalidOperationException>();
    }

    private Karamchari.Recruitment.Domain.Offer CreateInStatus(Karamchari.Recruitment.Domain.OfferStatus status)
    {
        var offer = Karamchari.Recruitment.Domain.Offer.Create(applicationId, baseSalary, currency);
        if (status == Karamchari.Recruitment.Domain.OfferStatus.Draft) return offer;

        if (status == Karamchari.Recruitment.Domain.OfferStatus.Declined)
        {
            offer.Decline();
            return offer;
        }

        offer.SubmitForApproval();
        if (status == Karamchari.Recruitment.Domain.OfferStatus.PendingApproval) return offer;

        offer.Approve();
        if (status == Karamchari.Recruitment.Domain.OfferStatus.Approved) return offer;

        if (status == Karamchari.Recruitment.Domain.OfferStatus.Issued)
        {
            offer.Issue(DateTimeOffset.UtcNow.AddDays(7));
            return offer;
        }

        if (status == Karamchari.Recruitment.Domain.OfferStatus.Accepted)
        {
            offer.Issue(DateTimeOffset.UtcNow.AddDays(7));
            offer.Accept();
            return offer;
        }

        return offer;
    }
}
