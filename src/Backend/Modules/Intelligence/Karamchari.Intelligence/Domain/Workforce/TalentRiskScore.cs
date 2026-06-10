// -----------------------------------------------------------------------
// <copyright file="TalentRiskScore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Composite talent risk: combines burnout, attrition, and shift dependency signals
/// to identify employees who are both high-risk AND high-value to lose.
/// One row per employee — upserted on each nightly run.
/// </summary>
public sealed class TalentRiskScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    /// <summary>Composite talent risk score in [0, 100].</summary>
    public decimal Score { get; private set; }

    public WorkforceRiskLevel RiskLevel { get; private set; }

    /// <summary>Burnout score component (0–100) at time of calculation.</summary>
    public decimal BurnoutComponent { get; private set; }

    /// <summary>Attrition score component (0–100) at time of calculation.</summary>
    public decimal AttritionComponent { get; private set; }

    /// <summary>Dependency risk component: fraction of critical shifts only this employee can cover × 100.</summary>
    public decimal DependencyComponent { get; private set; }

    /// <summary>Extra compound penalty applied when burnout ≥ 70 AND attrition ≥ 60.</summary>
    public decimal CompoundPenalty { get; private set; }

    public ScoreExplanation Explanation { get; private set; } = null!;

    public DateTime CalculatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private TalentRiskScore() { }

    public static TalentRiskScore Create(
        string tenantId,
        Guid employeeId,
        decimal score,
        WorkforceRiskLevel riskLevel,
        decimal burnoutComponent,
        decimal attritionComponent,
        decimal dependencyComponent,
        decimal compoundPenalty,
        ScoreExplanation explanation)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Score = score,
            RiskLevel = riskLevel,
            BurnoutComponent = burnoutComponent,
            AttritionComponent = attritionComponent,
            DependencyComponent = dependencyComponent,
            CompoundPenalty = compoundPenalty,
            Explanation = explanation,
            CalculatedAt = DateTime.UtcNow
        };

    public void Refresh(
        decimal score,
        WorkforceRiskLevel riskLevel,
        decimal burnoutComponent,
        decimal attritionComponent,
        decimal dependencyComponent,
        decimal compoundPenalty,
        ScoreExplanation explanation)
    {
        Score = score;
        RiskLevel = riskLevel;
        BurnoutComponent = burnoutComponent;
        AttritionComponent = attritionComponent;
        DependencyComponent = dependencyComponent;
        CompoundPenalty = compoundPenalty;
        Explanation = explanation;
        CalculatedAt = DateTime.UtcNow;
    }
}
