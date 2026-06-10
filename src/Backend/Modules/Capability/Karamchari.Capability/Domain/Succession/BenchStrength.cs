// -----------------------------------------------------------------------
// <copyright file="BenchStrength.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Succession;

/// <summary>
/// Query model: per-band candidate counts for one critical position.
/// Derived at query time from <see cref="SuccessorCandidateProjection"/> — not persisted.
/// </summary>
public sealed record BenchStrength(
    Guid PositionId,
    PositionCriticality Criticality,
    int ReadyNowCount,
    int ReadySoonCount,
    int NeedsDevelopmentCount,
    SuccessionRiskLevel RiskLevel)
{
    /// <summary>Total tracked successor candidates regardless of band.</summary>
    public int TotalCandidates => ReadyNowCount + ReadySoonCount + NeedsDevelopmentCount;

    /// <summary>Derives risk level from the ReadyNow count.</summary>
    public static SuccessionRiskLevel DeriveRisk(int readyNowCount) => readyNowCount switch
    {
        0 => SuccessionRiskLevel.High,
        1 => SuccessionRiskLevel.Medium,
        _ => SuccessionRiskLevel.Low,
    };
}

/// <summary>
/// Query model: succession risk summary for one critical position.
/// Used in the executive succession risk dashboard.
/// </summary>
public sealed record SuccessionRiskSummary(
    Guid PositionId,
    Guid RoleRequirementId,
    string RoleTitle,
    PositionCriticality Criticality,
    int ReadyNowCount,
    SuccessionRiskLevel RiskLevel);
