using System.ComponentModel.DataAnnotations;

namespace Karamchari.Billing.Domain.Contracts;

public enum BillingType
{
    TimeAndMaterial = 0,
    FixedBid = 1,
    Retainer = 2
}

public enum BillingCycle
{
    Weekly = 0,
    Monthly = 1,
    Milestone = 2
}

public enum RateUnit
{
    Hour = 0,
    Day = 1
}

/// <summary>
/// The authoritative agreement between a Tenant and a Client for a specific Project.
/// Drives all revenue calculation and invoice generation.
/// </summary>
public sealed class BillingContract
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;

    public Guid ClientId { get; init; }
    public Guid ProjectId { get; init; }

    public BillingType BillingType { get; set; }
    public string Currency { get; set; } = "INR";

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public BillingCycle BillingCycle { get; set; }
    public bool IsActive { get; set; } = true;

    private readonly List<RateCard> _rateCards = new();
    public IReadOnlyCollection<RateCard> RateCards => _rateCards.AsReadOnly();

    public byte[]? RowVersion { get; private set; }

    public static BillingContract Create(string tenantId, Guid clientId, Guid projectId, BillingType type)
    {
        return new BillingContract
        {
            TenantId = tenantId,
            ClientId = clientId,
            ProjectId = projectId,
            BillingType = type
        };
    }

    public void AddRate(Guid roleId, decimal rate, RateUnit unit, DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        // Validation: No overlapping rates for the same role
        if (_rateCards.Any(r => r.RoleId == roleId && 
            (effectiveTo == null || r.EffectiveFrom <= effectiveTo) && 
            (r.EffectiveTo == null || effectiveFrom <= r.EffectiveTo)))
        {
            throw new InvalidOperationException($"Rate for role {roleId} already exists in this period.");
        }

        _rateCards.Add(new RateCard
        {
            ContractId = this.Id,
            RoleId = roleId,
            Rate = rate,
            Unit = unit,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        });
    }

    public decimal ResolveRate(Guid roleId, DateOnly workDate)
    {
        var rate = _rateCards
            .Where(r => r.RoleId == roleId && 
                        r.EffectiveFrom <= workDate && 
                        (r.EffectiveTo == null || r.EffectiveTo >= workDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (rate == null)
            throw new InvalidOperationException($"No active rate card found for role {roleId} on {workDate}.");

        return rate.Rate;
    }
}

public sealed class RateCard
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ContractId { get; init; }
    
    public Guid RoleId { get; init; }
    public decimal Rate { get; init; }
    public RateUnit Unit { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
}
