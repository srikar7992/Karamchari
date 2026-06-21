using Karamchari.Benefits.Domain.Eligibility;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Benefits.Domain.Plans;

public sealed class BenefitPlan : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<BenefitPlanTier> _tiers = [];
    private readonly List<EligibilityRule> _eligibilityRules = [];

    private BenefitPlan() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public BenefitPlanType Type { get; private set; }
    public string Carrier { get; private set; } = string.Empty;
    public BenefitPlanStatus Status { get; private set; }
    public IReadOnlyList<BenefitPlanTier> Tiers => _tiers.AsReadOnly();
    public IReadOnlyList<EligibilityRule> EligibilityRules => _eligibilityRules.AsReadOnly();

    public static BenefitPlan Create(string tenantId, string name, BenefitPlanType type, string carrier)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Type = type,
                   Carrier = carrier, Status = BenefitPlanStatus.Draft };

    public void Activate() => Status = BenefitPlanStatus.Active;
    public void AddTier(BenefitPlanTier tier) => _tiers.Add(tier);
    public void AddEligibilityRule(EligibilityRule rule) => _eligibilityRules.Add(rule);
}
