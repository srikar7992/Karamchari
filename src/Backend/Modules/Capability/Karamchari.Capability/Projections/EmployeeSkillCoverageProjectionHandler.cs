// -----------------------------------------------------------------------
// <copyright file="EmployeeSkillCoverageProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Karamchari.Capability.Services;
using Karamchari.Core.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Projections;

/// <summary>
/// Recomputes <see cref="EmployeeSkillCoverageProjection"/> rows for an employee when a skill
/// is validated. For each active <see cref="RoleSkillRequirement"/> that references the validated
/// skill, coverage is recalculated and upserted.
/// Fanout is bounded: only requirements that include the specific skill are touched.
/// </summary>
public sealed class EmployeeSkillCoverageProjectionHandler
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeSkillCoverageProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The capability module database context.</param>
    public EmployeeSkillCoverageProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles a skill validation event by ensuring the employee's capability profile is current
    /// and recomputing skill coverage against all active role requirements that include the skill.
    /// </summary>
    public async Task HandleAsync(SkillValidatedIntegrationEventV1 @event, CancellationToken cancellationToken)
    {
        var level = (SkillLevel)Math.Clamp(@event.Level, 0, (int)SkillLevel.Expert);

        // 1. Ensure CapabilityProfile is current with the validated skill
        var profile = await _dbContext.CapabilityProfiles
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(
                p => p.TenantId == @event.TenantId && p.EmployeeId == @event.EmployeeId,
                cancellationToken);

        if (profile is null)
        {
            profile = CapabilityProfile.Initialize(@event.TenantId, @event.EmployeeId);
            _dbContext.CapabilityProfiles.Add(profile);
        }

        var existingSkill = profile.Skills.FirstOrDefault(s => s.SkillId == @event.SkillId);
        if (existingSkill is null)
            profile.AddVerifiedSkill(@event.SkillId, level, $"event:{nameof(SkillValidatedIntegrationEventV1)}", "system");
        else if (existingSkill.Level != level)
            profile.UpdateSkillLevel(@event.SkillId, level, $"event:{nameof(SkillValidatedIntegrationEventV1)}", "system");

        // 2. Find active requirements that include this skill (bounded fanout)
        var matchingRequirements = await _dbContext.RoleSkillRequirements
            .Include(r => r.Skills)
            .Where(r => r.TenantId == @event.TenantId
                     && r.IsActive
                     && r.Skills.Any(s => s.SkillId == @event.SkillId))
            .ToListAsync(cancellationToken);

        if (matchingRequirements.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // 3. Load existing coverage projections for this employee + matched requirements
        var requirementIds = matchingRequirements.Select(r => r.Id).ToList();
        var existingProjections = await _dbContext.EmployeeSkillCoverageProjections
            .Where(p => p.TenantId == @event.TenantId
                     && p.EmployeeId == @event.EmployeeId
                     && requirementIds.Contains(p.RoleRequirementId))
            .ToListAsync(cancellationToken);

        // 4. Upsert coverage for each matching requirement
        foreach (var requirement in matchingRequirements)
        {
            var (matched, missing, total) = SkillCoverageCalculator.Compute(requirement, profile);

            var existing = existingProjections.FirstOrDefault(p => p.RoleRequirementId == requirement.Id);
            if (existing is not null)
            {
                existing.Update(matched, missing, total, requirement.RoleTitle);
            }
            else
            {
                _dbContext.EmployeeSkillCoverageProjections.Add(
                    EmployeeSkillCoverageProjection.Compute(
                        @event.TenantId,
                        @event.EmployeeId,
                        requirement.Id,
                        requirement.RoleTitle,
                        matched,
                        missing,
                        total));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
