// -----------------------------------------------------------------------
// <copyright file="CareerReadinessProjectionHandler.cs" company="Karamchari">
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
/// Recomputes <see cref="CareerReadinessProjection"/> rows when a skill is validated.
/// Computes directly from <see cref="CapabilityProfile"/> and <see cref="RoleSkillRequirement"/> —
/// does not depend on <see cref="EmployeeSkillCoverageProjection"/> or
/// <see cref="EmployeeSkillGapProjection"/> to avoid consumer ordering races.
/// Fanout is bounded: only requirements that include the validated skill are touched.
/// </summary>
public sealed class CareerReadinessProjectionHandler
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CareerReadinessProjectionHandler"/> class.
    /// </summary>
    public CareerReadinessProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles a skill validation event by recomputing career readiness for all active role
    /// requirements that reference the validated skill.
    /// </summary>
    public async Task HandleAsync(SkillValidatedIntegrationEventV1 @event, CancellationToken cancellationToken)
    {
        var level = (SkillLevel)Math.Clamp(@event.Level, 0, (int)SkillLevel.Expert);

        // 1. Ensure CapabilityProfile is current
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

        // 2. Find active requirements containing this skill (bounded fanout)
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

        // 3. Load existing readiness projections for this employee + matched requirements
        var requirementIds = matchingRequirements.Select(r => r.Id).ToList();
        var existingProjections = await _dbContext.CareerReadinessProjections
            .Where(p => p.TenantId == @event.TenantId
                     && p.EmployeeId == @event.EmployeeId
                     && requirementIds.Contains(p.RoleRequirementId))
            .ToListAsync(cancellationToken);

        // 4. Upsert readiness for each matching requirement
        foreach (var requirement in matchingRequirements)
        {
            var (coveragePercent, gapCount, criticalGapCount, readinessScore, band) =
                CareerReadinessCalculator.Compute(requirement, profile);

            var existing = existingProjections.FirstOrDefault(p => p.RoleRequirementId == requirement.Id);
            if (existing is not null)
            {
                existing.Update(requirement.RoleTitle, coveragePercent, gapCount, criticalGapCount, readinessScore, band);
            }
            else
            {
                _dbContext.CareerReadinessProjections.Add(
                    CareerReadinessProjection.Compute(
                        @event.TenantId,
                        @event.EmployeeId,
                        requirement.Id,
                        requirement.RoleTitle,
                        coveragePercent,
                        gapCount,
                        criticalGapCount,
                        readinessScore,
                        band));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
