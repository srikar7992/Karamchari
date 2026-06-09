using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Karamchari.Core.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Projections;

/// <summary>
/// Recomputes <see cref="EmployeeSkillGapProjection"/> rows when a skill is validated.
/// Gap rows are created when the employee is below the required level or missing the skill.
/// Gap rows are deleted when the employee meets or exceeds the required level (gap closed).
/// Fanout is bounded: only requirements that include the validated skill are touched.
/// </summary>
public sealed class EmployeeSkillGapProjectionHandler
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeSkillGapProjectionHandler"/> class.
    /// </summary>
    public EmployeeSkillGapProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles a skill validation event by recomputing per-skill gaps for all active role
    /// requirements that reference the validated skill.
    /// </summary>
    public async Task HandleAsync(SkillValidatedIntegrationEvent @event, CancellationToken cancellationToken)
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
            profile.AddVerifiedSkill(@event.SkillId, level, $"event:{nameof(SkillValidatedIntegrationEvent)}", "system");
        else if (existingSkill.Level != level)
            profile.UpdateSkillLevel(@event.SkillId, level, $"event:{nameof(SkillValidatedIntegrationEvent)}", "system");

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

        // 3. Load existing gap rows for this employee + matched requirements
        var requirementIds = matchingRequirements.Select(r => r.Id).ToList();
        var existingGaps = await _dbContext.EmployeeSkillGapProjections
            .Where(g => g.TenantId == @event.TenantId
                     && g.EmployeeId == @event.EmployeeId
                     && requirementIds.Contains(g.RoleRequirementId))
            .ToListAsync(cancellationToken);

        // 4. For each requirement: diff current gaps against desired state
        foreach (var requirement in matchingRequirements)
        {
            var desiredGaps = requirement.ComputeGaps(profile);
            var existingForReq = existingGaps
                .Where(g => g.RoleRequirementId == requirement.Id)
                .ToList();

            // Upsert new/changed gaps
            foreach (var gap in desiredGaps)
            {
                var existing = existingForReq.FirstOrDefault(g => g.SkillId == gap.SkillId);
                if (existing is not null)
                {
                    existing.Update(requirement.RoleTitle, gap);
                }
                else
                {
                    _dbContext.EmployeeSkillGapProjections.Add(
                        EmployeeSkillGapProjection.FromGap(
                            @event.TenantId,
                            @event.EmployeeId,
                            requirement.Id,
                            requirement.RoleTitle,
                            gap));
                }
            }

            // Delete gap rows for skills that are now met (gap closed)
            var desiredSkillIds = new HashSet<Guid>(desiredGaps.Select(g => g.SkillId));
            var closedGaps = existingForReq.Where(g => !desiredSkillIds.Contains(g.SkillId));
            _dbContext.EmployeeSkillGapProjections.RemoveRange(closedGaps);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
