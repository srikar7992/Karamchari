using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain.Offers;

/// <summary>
/// Authoritative business status for recruitment offers.
/// Coordination states (Pending Approval) are managed by Workflow.
/// </summary>
public enum OfferStatus
{
    /// <summary>Offer is being drafted.</summary>
    Draft = 1,

    /// <summary>Offer has been approved and can be extended to candidate.</summary>
    Approved = 2,

    /// <summary>Offer has been sent to the candidate.</summary>
    Extended = 3,

    /// <summary>Candidate has accepted the offer.</summary>
    Accepted = 4,

    /// <summary>Candidate has rejected the offer.</summary>
    Rejected = 5,

    /// <summary>Offer has expired.</summary>
    Expired = 6,

    /// <summary>Offer was revoked by the company.</summary>
    Revoked = 7
}

/// <summary>
/// Aggregate root representing an employment offer.
/// Business truth is owned here; coordination is managed by Workflow.
/// </summary>
public sealed class OfferLetter : AggregateRoot<Guid>, ITenantOwned
{
    private OfferLetter() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ApplicationId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CandidateId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid RequisitionId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal BaseSalary { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;
    /// <inheritdoc/>
    public decimal? EquityGrant { get; private set; }
    /// <inheritdoc/>
    public decimal? SignOnBonus { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public OfferStatus Status { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static OfferLetter Create(
        string tenantId,
        Guid applicationId,
        Guid candidateId,
        Guid requisitionId,
        decimal baseSalary,
        string currency,
        decimal? equityGrant = null,
        decimal? signOnBonus = null)
    {
        var offer = new OfferLetter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationId = applicationId,
            CandidateId = candidateId,
            RequisitionId = requisitionId,
            BaseSalary = baseSalary,
            Currency = currency,
            EquityGrant = equityGrant,
            SignOnBonus = signOnBonus,
            Status = OfferStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return offer;
    }

    /// <summary>
    /// Finalizes the offer as approved, making it authoritative truth for extending.
    /// Called by Workflow coordination upon successful completion.
    /// </summary>
    public void FinalizeApproved()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot finalize offer in status {Status}");

        Status = OfferStatus.Approved;
    }

    /// <summary>
    /// Rejects the offer during coordination.
    /// </summary>
    public void FinalizeRejected()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot reject offer in status {Status}");

        Status = OfferStatus.Rejected;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Extend(TimeSpan validityPeriod)
    {
        if (Status != OfferStatus.Approved)
            throw new InvalidOperationException($"Cannot extend an offer in state {Status}");

        Status = OfferStatus.Extended;
        ExpiresAtUtc = DateTimeOffset.UtcNow.Add(validityPeriod);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Accept()
    {
        if (Status != OfferStatus.Extended)
            throw new InvalidOperationException($"Cannot accept offer in state {Status}");

        if (ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            Status = OfferStatus.Expired;
            throw new InvalidOperationException("Offer has expired.");
        }

        Status = OfferStatus.Accepted;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Revoke()
    {
        if (Status is OfferStatus.Accepted or OfferStatus.Rejected)
            throw new InvalidOperationException($"Cannot revoke offer in state {Status}");

        Status = OfferStatus.Revoked;
    }
}
