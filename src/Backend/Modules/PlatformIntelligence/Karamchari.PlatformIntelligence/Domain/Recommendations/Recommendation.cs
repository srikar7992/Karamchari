using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Recommendations;

public sealed class Recommendation : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string ActionJson { get; private set; } = string.Empty;
    public decimal ConfidenceScore { get; private set; }
    public decimal EstimatedSavings { get; private set; }
    public decimal RiskReductionPercent { get; private set; }
    public string ExpectedBenefitJson { get; private set; } = string.Empty;
    public RecommendationStatus Status { get; private set; }
    public Guid? LinkedRiskId { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    private Recommendation() { }

    public static Recommendation Create(
        string tenantId,
        string category,
        string title,
        string actionJson,
        decimal confidenceScore,
        decimal estimatedSavings,
        decimal riskReductionPercent,
        string expectedBenefitJson,
        Guid? linkedRiskId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Category = category,
            Title = title,
            ActionJson = actionJson,
            ConfidenceScore = confidenceScore,
            EstimatedSavings = estimatedSavings,
            RiskReductionPercent = riskReductionPercent,
            ExpectedBenefitJson = expectedBenefitJson,
            Status = RecommendationStatus.Active,
            LinkedRiskId = linkedRiskId,
            GeneratedAt = DateTime.UtcNow
        };

    public void Acknowledge() => Status = RecommendationStatus.Acknowledged;
    public void Dismiss() => Status = RecommendationStatus.Dismissed;
}
