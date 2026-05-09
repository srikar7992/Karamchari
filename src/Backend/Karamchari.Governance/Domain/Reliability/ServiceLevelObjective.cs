using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Reliability;

public enum AvailabilityClassification
{
    Tier1_Critical,   // 99.99% - Core Auth, Operational DBs
    Tier2_High,       // 99.9%  - BFFs, Main Dashboards
    Tier3_Medium,     // 99.5%  - Background Jobs, Integrations
    Tier4_BestEffort  // 99.0%  - Analytics Projections
}

/// <summary>
/// Aggregate root governing the reliability expectations for a specific platform service or workflow.
/// Tracks Error Budgets to enforce safe scaling velocity vs stability.
/// </summary>
public sealed class ServiceLevelObjective : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty; // "System" for global platform
    public string ComponentName { get; private set; } = string.Empty;
    public AvailabilityClassification Tier { get; private set; }
    
    public decimal TargetSuccessRate { get; private set; } // e.g., 99.9m
    public int EvaluationWindowDays { get; private set; } = 30;

    // Error Budget Tracking
    public decimal CurrentSuccessRate { get; private set; }
    public decimal ErrorBudgetRemainingPercent { get; private set; }
    public bool IsErrorBudgetExhausted => ErrorBudgetRemainingPercent <= 0;

    public DateTimeOffset LastEvaluatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private ServiceLevelObjective() { }

    public static ServiceLevelObjective Define(
        string tenantId,
        string componentName,
        AvailabilityClassification tier,
        decimal targetRate,
        int windowDays = 30)
    {
        return new ServiceLevelObjective
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ComponentName = componentName,
            Tier = tier,
            TargetSuccessRate = targetRate,
            EvaluationWindowDays = windowDays,
            CurrentSuccessRate = 100m,
            ErrorBudgetRemainingPercent = 100m,
            LastEvaluatedUtc = DateTimeOffset.UtcNow
        };
    }

    public void Evaluate(decimal measuredSuccessRate)
    {
        CurrentSuccessRate = measuredSuccessRate;
        
        var allowedErrorBudget = 100m - TargetSuccessRate;
        var actualErrorRate = 100m - measuredSuccessRate;
        
        // Calculate remaining budget as a percentage of the allowed budget
        if (allowedErrorBudget > 0)
        {
            ErrorBudgetRemainingPercent = Math.Max(0, ((allowedErrorBudget - actualErrorRate) / allowedErrorBudget) * 100m);
        }
        else
        {
            ErrorBudgetRemainingPercent = measuredSuccessRate >= 100m ? 100m : 0m;
        }

        LastEvaluatedUtc = DateTimeOffset.UtcNow;

        if (IsErrorBudgetExhausted)
        {
            // Raise ErrorBudgetExhaustedEvent -> Triggers CI/CD deployment freeze
        }
    }
}
