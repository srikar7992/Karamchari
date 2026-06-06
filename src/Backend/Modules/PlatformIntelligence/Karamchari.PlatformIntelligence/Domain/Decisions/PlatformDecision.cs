using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Decisions;

public sealed class PlatformDecision : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string ScopeKey { get; private set; } = string.Empty;
    public DecisionPriority Priority { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string RecommendationsJson { get; private set; } = string.Empty;
    public string ConflictsJson { get; private set; } = string.Empty;
    public decimal ConfidenceScore { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    private PlatformDecision() { }

    public static PlatformDecision Create(
        string tenantId,
        string scopeKey,
        DecisionPriority priority,
        string title,
        string recommendationsJson,
        string conflictsJson,
        decimal confidenceScore)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            Priority = priority,
            Title = title,
            RecommendationsJson = recommendationsJson,
            ConflictsJson = conflictsJson,
            ConfidenceScore = confidenceScore,
            GeneratedAt = DateTime.UtcNow
        };
}
