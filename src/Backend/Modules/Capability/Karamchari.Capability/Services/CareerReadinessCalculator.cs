using System;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;

namespace Karamchari.Capability.Services;

/// <summary>
/// Pure, EF-isolated calculator for career readiness scores.
/// No database access. All inputs must be pre-loaded by the caller.
/// </summary>
public static class CareerReadinessCalculator
{
    /// <summary>
    /// Number of levels behind the requirement for a gap to be considered critical.
    /// Gaps at this distance or greater penalise the readiness score at double weight.
    /// </summary>
    public const int CriticalGapLevelThreshold = 2;

    /// <summary>Score penalty applied per critical gap (missing or >= 2 levels behind).</summary>
    public const decimal CriticalGapPenalty = 5m;

    /// <summary>
    /// Computes career readiness for an employee against a single role skill requirement.
    /// A gap is critical when the skill is missing entirely or the employee is
    /// <see cref="CriticalGapLevelThreshold"/> or more levels behind the requirement.
    /// </summary>
    /// <param name="requirement">The role skill requirement defining the target role.</param>
    /// <param name="profile">The employee's capability profile with verified skills.</param>
    /// <returns>
    /// A tuple of (CoveragePercent, GapCount, CriticalGapCount, ReadinessScore, Band).
    /// </returns>
    public static (
        decimal CoveragePercent,
        int GapCount,
        int CriticalGapCount,
        decimal ReadinessScore,
        CareerReadinessBand Band) Compute(
        RoleSkillRequirement requirement,
        CapabilityProfile profile)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(profile);

        var (matched, missing, total) = SkillCoverageCalculator.Compute(requirement, profile);

        decimal coveragePercent = total > 0
            ? Math.Round((decimal)matched / total * 100m, 4)
            : 100m;

        // Count critical gaps from per-skill gap analysis
        int criticalGapCount = 0;
        int gapCount = 0;
        var gaps = requirement.ComputeGaps(profile);
        foreach (var gap in gaps)
        {
            gapCount++;
            if (gap.IsMissing || gap.LevelsBehind >= CriticalGapLevelThreshold)
                criticalGapCount++;
        }

        decimal readinessScore = Math.Max(0m,
            Math.Round(coveragePercent - criticalGapCount * CriticalGapPenalty, 4));

        var band = DetermineBand(coveragePercent, criticalGapCount);

        return (coveragePercent, gapCount, criticalGapCount, readinessScore, band);
    }

    private static CareerReadinessBand DetermineBand(decimal coveragePercent, int criticalGapCount)
    {
        if (coveragePercent >= 90m && criticalGapCount == 0)
            return CareerReadinessBand.ReadyNow;

        if (coveragePercent >= 70m && criticalGapCount <= 2)
            return CareerReadinessBand.ReadySoon;

        return CareerReadinessBand.NeedsDevelopment;
    }
}
