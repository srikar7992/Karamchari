using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class Offer : AggregateRoot<OfferId>, ITenantOwned, IAuditable
{
    private Offer() : base(default) { Currency = null!; }

    private Offer(OfferId id, ApplicationId applicationId, decimal baseSalary, string currency)
        : base(id)
    {
        ApplicationId = applicationId;
        BaseSalary = baseSalary;
        Currency = currency;
        Status = OfferStatus.Draft;
    }

    public ApplicationId ApplicationId { get; private set; }
    public decimal BaseSalary { get; private set; }
    public string Currency { get; private set; }
    public OfferStatus Status { get; private set; }
    public DateTimeOffset? IssuedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static Offer Create(ApplicationId applicationId, decimal baseSalary, string currency)
    {
        var offer = new Offer(OfferId.New(), applicationId, baseSalary, currency);
        offer.RaiseDomainEvent(new OfferDraftedDomainEvent(offer.Id, applicationId));
        return offer;
    }

    public void SubmitForApproval()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException("Only draft offers can be submitted for approval.");

        Status = OfferStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != OfferStatus.PendingApproval)
            throw new InvalidOperationException("Only offers pending approval can be approved.");

        Status = OfferStatus.Approved;
        RaiseDomainEvent(new OfferApprovedDomainEvent(Id));
    }

    public void Issue(DateTimeOffset expiresAt)
    {
        if (Status != OfferStatus.Approved)
            throw new InvalidOperationException("Only approved offers can be issued.");

        Status = OfferStatus.Issued;
        IssuedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        RaiseDomainEvent(new OfferIssuedDomainEvent(Id));
    }

    public void Accept()
    {
        if (Status != OfferStatus.Issued)
            throw new InvalidOperationException("Only issued offers can be accepted.");

        if (ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value)
            throw new InvalidOperationException("Offer has expired.");

        Status = OfferStatus.Accepted;
        RaiseDomainEvent(new OfferAcceptedDomainEvent(Id));
    }

    public void Decline()
    {
        Status = OfferStatus.Declined;
        RaiseDomainEvent(new OfferDeclinedDomainEvent(Id));
    }
}
