using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Domain.Workflows;

namespace Karamchari.Recruitment.Domain.Offers;

public enum OfferStatus
{
    Draft,
    PendingApproval,
    Approved,
    Extended,
    Accepted,
    Rejected,
    Expired,
    Revoked
}

/// <summary>
/// Aggregate root representing an employment offer.
/// Orchestrates compensation approvals and candidate acceptance.
/// </summary>
public sealed class OfferLetter : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ApprovalStep> _approvalChain = [];

    public string TenantId { get; private set; } = string.Empty;
    public Guid ApplicationId { get; private set; }
    public Guid CandidateId { get; private set; }
    public Guid RequisitionId { get; private set; }
    
    public decimal BaseSalary { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal? EquityGrant { get; private set; }
    public decimal? SignOnBonus { get; private set; }
    
    public OfferStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ApprovalStep> ApprovalChain => _approvalChain.AsReadOnly();

    private OfferLetter() { }

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

        // Standard offer approval chain: Recruiter -> Hiring Manager -> Finance
        offer._approvalChain.Add(ApprovalStep.Create(1, "Recruiter"));
        offer._approvalChain.Add(ApprovalStep.Create(2, "Hiring Manager"));
        offer._approvalChain.Add(ApprovalStep.Create(3, "Finance"));

        return offer;
    }

    public void RequestApproval()
    {
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot request approval from state {Status}");

        Status = OfferStatus.PendingApproval;
    }

    public void Approve(string approverId, string? note = null)
    {
        if (Status != OfferStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve offer in status {Status}");

        var currentStep = _approvalChain
            .OrderBy(s => s.Sequence)
            .FirstOrDefault(s => s.Status == ApprovalStatus.Pending)
            ?? throw new InvalidOperationException("No pending approval steps found.");

        currentStep.Approve(approverId, note);

        if (_approvalChain.All(s => s.Status == ApprovalStatus.Approved))
        {
            Status = OfferStatus.Approved;
        }
    }

    public void Extend(TimeSpan validityPeriod)
    {
        if (Status != OfferStatus.Approved)
            throw new InvalidOperationException($"Cannot extend an offer in state {Status}");

        Status = OfferStatus.Extended;
        ExpiresAtUtc = DateTimeOffset.UtcNow.Add(validityPeriod);
    }

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

    public void Reject(string reason)
    {
        if (Status != OfferStatus.Extended)
            throw new InvalidOperationException($"Cannot reject offer in state {Status}");

        Status = OfferStatus.Rejected;
    }
}
