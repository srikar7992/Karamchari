// -----------------------------------------------------------------------
// <copyright file="ServiceLevelObjective.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Governance.Domain.Reliability;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum AvailabilityClassification
{
    /// <inheritdoc/>
    Tier1Critical,   // 99.99% - Core Auth, Operational DBs
    /// <inheritdoc/>
    Tier2High,       // 99.9%  - BFFs, Main Dashboards
    /// <inheritdoc/>
    Tier3Medium,     // 99.5%  - Background Jobs, Integrations
    /// <inheritdoc/>
    Tier4BestEffort  // 99.0%  - Analytics Projections
}

/// <summary>
/// Aggregate root governing the reliability expectations for a specific platform service or workflow.
/// Tracks Error Budgets to enforce safe scaling velocity vs stability.
/// </summary>
public sealed class ServiceLevelObjective : AggregateRoot<Guid>, ITenantOwned
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty; // "System" for global platform
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ComponentName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public AvailabilityClassification Tier { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TargetSuccessRate { get; private set; } // e.g., 99.9m
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int EvaluationWindowDays { get; private set; } = 30;

    // Error Budget Tracking
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal CurrentSuccessRate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ErrorBudgetRemainingPercent { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsErrorBudgetExhausted => ErrorBudgetRemainingPercent <= 0;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastEvaluatedUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private ServiceLevelObjective() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
