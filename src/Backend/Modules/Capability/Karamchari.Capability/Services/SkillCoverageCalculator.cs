using System;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;

namespace Karamchari.Capability.Services;

/// <summary>
/// Pure, EF-isolated calculator for skill coverage scores.
/// No database access. All inputs must be pre-loaded by the caller.
/// </summary>
public static class SkillCoverageCalculator
{
    /// <summary>
    /// Computes coverage statistics for an employee against a single role skill requirement.
    /// Only mandatory skills are counted. Optional skills are ignored.
    /// </summary>
    /// <param name="requirement">The role skill requirement defining the target role.</param>
    /// <param name="profile">The employee's capability profile with verified skills.</param>
    /// <returns>A tuple of (matched, missing, total) mandatory skill counts.</returns>
    public static (int Matched, int Missing, int Total) Compute(
        RoleSkillRequirement requirement,
        CapabilityProfile profile)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(profile);

        var mandatorySkills = requirement.Skills;
        int matched = 0;
        int missing = 0;

        foreach (var req in mandatorySkills)
        {
            if (!req.IsMandatory)
                continue;

            var verified = profile.Skills
                .FirstOrDefault(s => s.SkillId == req.SkillId);

            if (verified != null && verified.Level >= req.MinimumLevel)
                matched++;
            else
                missing++;
        }

        int total = matched + missing;
        return (matched, missing, total);
    }
}
