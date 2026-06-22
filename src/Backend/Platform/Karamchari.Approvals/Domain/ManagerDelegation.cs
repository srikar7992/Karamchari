using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Approvals.Domain;

public sealed class ManagerDelegation : AggregateRoot<Guid>, ITenantOwned
{
    private ManagerDelegation() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid DelegatingManagerId { get; private set; }
    public Guid DelegateManagerId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ManagerDelegation Create(
        string tenantId,
        Guid delegatingManagerId,
        Guid delegateManagerId,
        DateOnly effectiveFrom,
        DateOnly effectiveTo)
    {
        if (delegatingManagerId == delegateManagerId)
            throw new ArgumentException("Cannot delegate to self.");
        if (effectiveTo < effectiveFrom)
            throw new ArgumentException("EffectiveTo must be >= EffectiveFrom.");

        var d = new ManagerDelegation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DelegatingManagerId = delegatingManagerId,
            DelegateManagerId = delegateManagerId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        d.RaiseDomainEvent(new DelegationCreated(
            d.Id, tenantId, delegatingManagerId, delegateManagerId, effectiveFrom, effectiveTo));
        return d;
    }

    public void Revoke() => IsActive = false;
}
