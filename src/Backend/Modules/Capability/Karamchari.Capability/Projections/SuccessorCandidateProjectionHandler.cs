// -----------------------------------------------------------------------
// <copyright file="SuccessorCandidateProjectionHandler.cs" company="Karamchari">
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
using Karamchari.Capability.Domain.Succession;
using Karamchari.Capability.Persistence;
using Karamchari.Capability.Services;
using Karamchari.Core.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Projections;

/// <summary>
/// Recomputes <see cref="SuccessorCandidateProjection"/> rows when a skill is validated.
/// Reads from <see cref="CriticalPosition"/> to find which positions are affected, then computes
/// readiness directly from <see cref="CapabilityProfile"/> and <see cref="RoleSkillRequirement"/>
/// — no dependency on other projections. Re-ranks all candidates per affected position.
/// </summary>
public sealed class SuccessorCandidateProjectionHandler
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessorCandidateProjectionHandler"/> class.
    /// </summary>
    public SuccessorCandidateProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles a skill validation event by updating successor readiness for all active critical
    /// positions whose role requirement includes the validated skill.
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

        // 2. Find active critical positions whose role requirement includes this skill
        var affectedPositions = await _dbContext.CriticalPositions
            .Where(cp => cp.TenantId == @event.TenantId && cp.IsActive)
            .Join(
                _dbContext.RoleSkillRequirements.Include(r => r.Skills)
                    .Where(r => r.TenantId == @event.TenantId
                             && r.IsActive
                             && r.Skills.Any(s => s.SkillId == @event.SkillId)),
                cp => cp.RoleRequirementId,
                r => r.Id,
                (cp, r) => new { CriticalPosition = cp, Requirement = r })
            .ToListAsync(cancellationToken);

        if (affectedPositions.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // 3. Upsert this employee's readiness for each affected position
        var positionIds = affectedPositions.Select(x => x.CriticalPosition.PositionId).ToList();
        var existingRow = await _dbContext.SuccessorCandidateProjections
            .Where(p => p.TenantId == @event.TenantId
                     && p.EmployeeId == @event.EmployeeId
                     && positionIds.Contains(p.PositionId))
            .ToListAsync(cancellationToken);

        foreach (var item in affectedPositions)
        {
            var (_, _, criticalGapCount, readinessScore, band) =
                CareerReadinessCalculator.Compute(item.Requirement, profile);

            var existing = existingRow.FirstOrDefault(p => p.PositionId == item.CriticalPosition.PositionId);
            if (existing is not null)
            {
                existing.Update(readinessScore, band);
            }
            else
            {
                // Rank placeholder (1); re-ranked below after all upserts
                _dbContext.SuccessorCandidateProjections.Add(
                    SuccessorCandidateProjection.Create(
                        @event.TenantId,
                        item.CriticalPosition.PositionId,
                        @event.EmployeeId,
                        readinessScore,
                        band,
                        rank: 1));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Re-rank all candidates for each affected position (bounded: typically < 50 per position)
        foreach (var positionId in positionIds)
        {
            var allCandidates = await _dbContext.SuccessorCandidateProjections
                .Where(p => p.TenantId == @event.TenantId && p.PositionId == positionId)
                .OrderByDescending(p => p.ReadinessScore)
                .ToListAsync(cancellationToken);

            for (var i = 0; i < allCandidates.Count; i++)
                allCandidates[i].ApplyRank(i + 1);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
