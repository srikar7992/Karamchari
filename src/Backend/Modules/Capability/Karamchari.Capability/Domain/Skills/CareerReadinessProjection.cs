// -----------------------------------------------------------------------
// <copyright file="CareerReadinessProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Pre-computed career readiness score for one employee against one role skill requirement.
/// Combines coverage percentage with critical gap weighting to produce a ranking signal for
/// internal mobility, career pathing, succession planning, and talent marketplace.
/// PK: (TenantId, EmployeeId, RoleRequirementId) — same grain as <see cref="EmployeeSkillCoverageProjection"/>.
/// </summary>
public sealed class CareerReadinessProjection
{
    private CareerReadinessProjection() { }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the employee identifier.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Gets the role skill requirement identifier this readiness was computed against.</summary>
    public Guid RoleRequirementId { get; private set; }

    /// <summary>Gets the role title at the time this projection was computed.</summary>
    public string RoleTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the percentage of mandatory required skills met or exceeded by the employee.
    /// Range: 0.0000 – 100.0000.
    /// </summary>
    public decimal CoveragePercent { get; private set; }

    /// <summary>Gets the total number of mandatory skill gaps (missing or below required level).</summary>
    public int GapCount { get; private set; }

    /// <summary>
    /// Gets the number of critical gaps: skills that are missing entirely or two or more levels behind.
    /// Critical gaps penalise the readiness score more heavily than shallow gaps.
    /// </summary>
    public int CriticalGapCount { get; private set; }

    /// <summary>
    /// Gets the readiness score (0–100). Derived from coverage minus critical-gap penalty.
    /// Use for ORDER BY in mobility and succession ranking queries.
    /// </summary>
    public decimal ReadinessScore { get; private set; }

    /// <summary>Gets the readiness band for quick categorical filtering.</summary>
    public CareerReadinessBand Band { get; private set; }

    /// <summary>Gets the UTC timestamp when this projection was last recomputed.</summary>
    public DateTimeOffset LastUpdatedUtc { get; private set; }

    /// <summary>Creates a new projection row from pre-computed readiness values.</summary>
    public static CareerReadinessProjection Compute(
        string tenantId,
        Guid employeeId,
        Guid roleRequirementId,
        string roleTitle,
        decimal coveragePercent,
        int gapCount,
        int criticalGapCount,
        decimal readinessScore,
        CareerReadinessBand band)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new CareerReadinessProjection
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            RoleRequirementId = roleRequirementId,
            RoleTitle = roleTitle,
            CoveragePercent = coveragePercent,
            GapCount = gapCount,
            CriticalGapCount = criticalGapCount,
            ReadinessScore = readinessScore,
            Band = band,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Updates this projection in-place with recomputed values.</summary>
    public void Update(
        string roleTitle,
        decimal coveragePercent,
        int gapCount,
        int criticalGapCount,
        decimal readinessScore,
        CareerReadinessBand band)
    {
        RoleTitle = roleTitle;
        CoveragePercent = coveragePercent;
        GapCount = gapCount;
        CriticalGapCount = criticalGapCount;
        ReadinessScore = readinessScore;
        Band = band;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
