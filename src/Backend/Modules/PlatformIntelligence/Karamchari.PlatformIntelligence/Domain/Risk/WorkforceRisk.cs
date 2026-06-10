// -----------------------------------------------------------------------
// <copyright file="WorkforceRisk.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Risk;

public sealed class WorkforceRisk : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public RiskCategory Category { get; private set; }
    public RiskLevel Level { get; private set; }
    public decimal RiskScore { get; private set; }
    public string RationaleJson { get; private set; } = string.Empty;
    public DateTime EvaluatedAt { get; private set; }

    private WorkforceRisk() { }

    public static WorkforceRisk Create(
        string tenantId,
        RiskCategory category,
        RiskLevel level,
        decimal riskScore,
        string rationaleJson)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Category = category,
            Level = level,
            RiskScore = riskScore,
            RationaleJson = rationaleJson,
            EvaluatedAt = DateTime.UtcNow
        };

    public void Refresh(RiskLevel level, decimal riskScore, string rationaleJson)
    {
        Level = level;
        RiskScore = riskScore;
        RationaleJson = rationaleJson;
        EvaluatedAt = DateTime.UtcNow;
    }
}
