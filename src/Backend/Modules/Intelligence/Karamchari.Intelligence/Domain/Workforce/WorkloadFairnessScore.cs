// -----------------------------------------------------------------------
// <copyright file="WorkloadFairnessScore.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Team/site-level workload fairness: measures whether overtime, shifts, holidays,
/// and weekend duty are distributed equitably across team members.
/// One row per (tenant, siteCode, teamId) — upserted nightly.
/// </summary>
public sealed class WorkloadFairnessScore : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Site code or "*" for tenant-wide aggregate.</summary>
    public string SiteCode { get; private set; } = string.Empty;

    /// <summary>Team identifier. Null = site-level aggregate.</summary>
    public Guid? TeamId { get; private set; }

    /// <summary>Overall fairness in [0, 100]. Higher = more equitable distribution.</summary>
    public decimal OverallScore { get; private set; }

    /// <summary>OT Gini coefficient [0, 1]. 0 = perfect equality, 1 = one person does all OT.</summary>
    public decimal OtGiniCoefficient { get; private set; }

    /// <summary>Fraction of total OT held by the top 10% OT earners.</summary>
    public decimal OtConcentrationTop10Pct { get; private set; }

    /// <summary>Number of team members analysed.</summary>
    public int TeamSize { get; private set; }

    /// <summary>Average OT hours per member in the analysis window.</summary>
    public decimal AvgOtHoursPerMember { get; private set; }

    /// <summary>Max OT hours for any single member in the window.</summary>
    public decimal MaxOtHoursAnyMember { get; private set; }

    public DateTime CalculatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private WorkloadFairnessScore() { }

    public static WorkloadFairnessScore Create(
        string tenantId,
        string siteCode,
        Guid? teamId,
        decimal overallScore,
        decimal otGini,
        decimal otConcentration,
        int teamSize,
        decimal avgOt,
        decimal maxOt)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SiteCode = siteCode,
            TeamId = teamId,
            OverallScore = overallScore,
            OtGiniCoefficient = otGini,
            OtConcentrationTop10Pct = otConcentration,
            TeamSize = teamSize,
            AvgOtHoursPerMember = avgOt,
            MaxOtHoursAnyMember = maxOt,
            CalculatedAt = DateTime.UtcNow
        };

    public void Refresh(
        decimal overallScore,
        decimal otGini,
        decimal otConcentration,
        int teamSize,
        decimal avgOt,
        decimal maxOt)
    {
        OverallScore = overallScore;
        OtGiniCoefficient = otGini;
        OtConcentrationTop10Pct = otConcentration;
        TeamSize = teamSize;
        AvgOtHoursPerMember = avgOt;
        MaxOtHoursAnyMember = maxOt;
        CalculatedAt = DateTime.UtcNow;
    }
}
