using Karamchari.Compliance.Contracts;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compliance.Domain.LegalHold;

/// <summary>
/// Freeze on data deletion for litigation, government inquiry, or dispute.
/// Overrides retention policy — records under hold must not be deleted.
/// </summary>
public sealed class LegalHold : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LegalHoldScope> _scopes = [];

    public string TenantId { get; private set; } = string.Empty;
    public string CaseReference { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public LegalHoldStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? ReleasedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<LegalHoldScope> Scopes => _scopes.AsReadOnly();

    private LegalHold() { }

    public static LegalHold Create(
        string tenantId,
        string caseReference,
        string reason,
        DateTime startDate,
        string createdBy)
    {
        var hold = new LegalHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseReference = caseReference,
            Reason = reason,
            StartDate = startDate,
            Status = LegalHoldStatus.Active,
            CreatedBy = createdBy
        };
        hold.RaiseDomainEvent(new LegalHoldCreatedEvent(hold.Id, tenantId, caseReference, reason, startDate));
        return hold;
    }

    public void AddScope(LegalHoldScopeType scopeType, string scopeValue)
    {
        _scopes.Add(LegalHoldScope.Create(Id, scopeType, scopeValue));
    }

    public void Release(DateTime releasedAt)
    {
        Status = LegalHoldStatus.Released;
        ReleasedAt = releasedAt;
        RaiseDomainEvent(new LegalHoldReleasedEvent(Id, TenantId, CaseReference, releasedAt));
    }

    public void Expire()
    {
        Status = LegalHoldStatus.Expired;
    }
}
