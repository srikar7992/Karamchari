// -----------------------------------------------------------------------
// <copyright file="WorkforceScenario.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.PlatformIntelligence.Domain.Scenarios;

public sealed class WorkforceScenario : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ScenarioStatus Status { get; private set; }
    public string ConstraintsJson { get; private set; } = string.Empty;
    public string? ResultJson { get; private set; }
    public decimal? CoverageScore { get; private set; }
    public decimal? OvertimeRate { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public bool IsOptimal { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EvaluatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private WorkforceScenario() { }

    public static WorkforceScenario Create(
        string tenantId,
        string name,
        string description,
        string constraintsJson,
        string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            Status = ScenarioStatus.Pending,
            ConstraintsJson = constraintsJson,
            IsOptimal = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public void MarkEvaluating() => Status = ScenarioStatus.Evaluating;

    public void MarkEvaluated(string resultJson, decimal coverageScore, decimal overtimeRate, decimal estimatedCost)
    {
        ResultJson = resultJson;
        CoverageScore = coverageScore;
        OvertimeRate = overtimeRate;
        EstimatedCost = estimatedCost;
        Status = ScenarioStatus.Evaluated;
        EvaluatedAt = DateTime.UtcNow;
    }

    public void MarkOptimal() => IsOptimal = true;

    public void MarkFailed() => Status = ScenarioStatus.Failed;
}
