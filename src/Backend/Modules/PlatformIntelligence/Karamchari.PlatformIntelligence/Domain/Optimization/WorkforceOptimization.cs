using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Optimization;

public sealed class WorkforceOptimization : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string ScopeKey { get; private set; } = string.Empty;
    public string ObjectiveType { get; private set; } = string.Empty;
    public string ConstraintsJson { get; private set; } = string.Empty;
    public string? RecommendationsJson { get; private set; }
    public decimal? EstimatedAnnualSavings { get; private set; }
    public OptimizationStatus Status { get; private set; }
    public DateTime GeneratedAt { get; private set; }

    private WorkforceOptimization() { }

    public static WorkforceOptimization Create(
        string tenantId,
        string scopeKey,
        string objectiveType,
        string constraintsJson)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ScopeKey = scopeKey,
            ObjectiveType = objectiveType,
            ConstraintsJson = constraintsJson,
            Status = OptimizationStatus.Pending,
            GeneratedAt = DateTime.UtcNow
        };

    public void MarkCompleted(string recommendationsJson, decimal estimatedAnnualSavings)
    {
        RecommendationsJson = recommendationsJson;
        EstimatedAnnualSavings = estimatedAnnualSavings;
        Status = OptimizationStatus.Completed;
    }

    public void MarkFailed() => Status = OptimizationStatus.Failed;
}
