// -----------------------------------------------------------------------
// <copyright file="InternalMobilityProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Capability.Domain.Mobility;
using Karamchari.Capability.Domain.Primitives;
using Karamchari.Capability.Domain.Skills;
using Karamchari.Capability.Persistence;
using Karamchari.Capability.Services;
using Karamchari.Core.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Capability.Projections;

/// <summary>
/// Maintains <see cref="InternalMobilityProjection"/> rows in response to vacancy and skill events.
/// <list type="bullet">
///   <item><see cref="HandleVacancyOpenedAsync"/> — vacancy opened: seed mobility rows from existing readiness projections.</item>
///   <item><see cref="HandleVacancyClosedAsync"/> — vacancy closed: delete mobility rows and vacancy index.</item>
///   <item><see cref="HandleSkillValidatedAsync"/> — skill validated: recompute mobility for affected open vacancies.
///     Computes directly from <see cref="CapabilityProfile"/> + <see cref="RoleSkillRequirement"/> to avoid
///     consumer ordering races with <see cref="CareerReadinessProjectionHandler"/>.</item>
/// </list>
/// </summary>
public sealed class InternalMobilityProjectionHandler
{
    private readonly CapabilityDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of <see cref="InternalMobilityProjectionHandler"/>.
    /// </summary>
    public InternalMobilityProjectionHandler(CapabilityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Seeds mobility rows from existing <see cref="CareerReadinessProjection"/> data when a vacancy opens.
    /// Skips vacancies with no linked role skill requirement.
    /// </summary>
    public async Task HandleVacancyOpenedAsync(VacancyOpenedIntegrationEventV1 @event, CancellationToken cancellationToken)
    {
        if (@event.RoleSkillRequirementId is null)
            return;

        var roleRequirementId = @event.RoleSkillRequirementId.Value;

        // Upsert MobilityVacancy index
        var existingVacancy = await _dbContext.MobilityVacancies
            .FirstOrDefaultAsync(
                v => v.TenantId == @event.TenantId && v.VacancyId == @event.VacancyId,
                cancellationToken);

        if (existingVacancy is null)
        {
            _dbContext.MobilityVacancies.Add(
                MobilityVacancy.Open(@event.TenantId, @event.VacancyId, @event.PositionId, roleRequirementId, @event.OpenedUtc));
        }

        // Seed mobility rows from settled CareerReadinessProjection rows for this requirement
        var readinessRows = await _dbContext.CareerReadinessProjections
            .Where(r => r.TenantId == @event.TenantId && r.RoleRequirementId == roleRequirementId)
            .ToListAsync(cancellationToken);

        if (readinessRows.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var employeeIds = readinessRows.Select(r => r.EmployeeId).ToList();
        var existingMobility = await _dbContext.InternalMobilityProjections
            .Where(m => m.TenantId == @event.TenantId
                     && m.VacancyId == @event.VacancyId
                     && employeeIds.Contains(m.EmployeeId))
            .ToListAsync(cancellationToken);

        foreach (var readiness in readinessRows)
        {
            var existing = existingMobility.FirstOrDefault(m => m.EmployeeId == readiness.EmployeeId);
            if (existing is not null)
            {
                existing.Update(
                    readiness.CoveragePercent,
                    readiness.ReadinessScore,
                    readiness.Band,
                    missingSkillCount: readiness.GapCount,
                    readiness.CriticalGapCount);
            }
            else
            {
                _dbContext.InternalMobilityProjections.Add(
                    InternalMobilityProjection.Create(
                        @event.TenantId,
                        readiness.EmployeeId,
                        @event.VacancyId,
                        @event.PositionId,
                        roleRequirementId,
                        readiness.CoveragePercent,
                        readiness.ReadinessScore,
                        readiness.Band,
                        missingSkillCount: readiness.GapCount,
                        readiness.CriticalGapCount));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes all mobility rows and the vacancy index when a vacancy closes.
    /// </summary>
    public async Task HandleVacancyClosedAsync(VacancyClosedIntegrationEventV1 @event, CancellationToken cancellationToken)
    {
        var mobilityRows = await _dbContext.InternalMobilityProjections
            .Where(m => m.TenantId == @event.TenantId && m.VacancyId == @event.VacancyId)
            .ToListAsync(cancellationToken);

        if (mobilityRows.Count > 0)
            _dbContext.InternalMobilityProjections.RemoveRange(mobilityRows);

        var vacancyIndex = await _dbContext.MobilityVacancies
            .FirstOrDefaultAsync(
                v => v.TenantId == @event.TenantId && v.VacancyId == @event.VacancyId,
                cancellationToken);

        if (vacancyIndex is not null)
            _dbContext.MobilityVacancies.Remove(vacancyIndex);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Recomputes mobility for open vacancies affected by a newly validated skill.
    /// Computes from <see cref="CapabilityProfile"/> + <see cref="RoleSkillRequirement"/> directly —
    /// does not read from <see cref="CareerReadinessProjection"/> to avoid consumer ordering races.
    /// </summary>
    public async Task HandleSkillValidatedAsync(SkillValidatedIntegrationEventV1 @event, CancellationToken cancellationToken)
    {
        var level = (SkillLevel)Math.Clamp(@event.Level, 0, (int)SkillLevel.Expert);

        // 1. Load / upsert CapabilityProfile
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

        // 2. Find requirements containing the validated skill
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

        // 3. Find open vacancies for those requirements
        var requirementIds = matchingRequirements.Select(r => r.Id).ToList();
        var openVacancies = await _dbContext.MobilityVacancies
            .Where(v => v.TenantId == @event.TenantId && requirementIds.Contains(v.RoleRequirementId))
            .ToListAsync(cancellationToken);

        if (openVacancies.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // 4. Load existing mobility projections for this employee × affected vacancies
        var vacancyIds = openVacancies.Select(v => v.VacancyId).ToList();
        var existingMobility = await _dbContext.InternalMobilityProjections
            .Where(m => m.TenantId == @event.TenantId
                     && m.EmployeeId == @event.EmployeeId
                     && vacancyIds.Contains(m.VacancyId))
            .ToListAsync(cancellationToken);

        // 5. Upsert per vacancy
        foreach (var vacancy in openVacancies)
        {
            var requirement = matchingRequirements.First(r => r.Id == vacancy.RoleRequirementId);
            var (coveragePercent, gapCount, criticalGapCount, readinessScore, band) =
                CareerReadinessCalculator.Compute(requirement, profile);

            var existing = existingMobility.FirstOrDefault(m => m.VacancyId == vacancy.VacancyId);
            if (existing is not null)
            {
                existing.Update(coveragePercent, readinessScore, band, gapCount, criticalGapCount);
            }
            else
            {
                _dbContext.InternalMobilityProjections.Add(
                    InternalMobilityProjection.Create(
                        @event.TenantId,
                        @event.EmployeeId,
                        vacancy.VacancyId,
                        vacancy.PositionId,
                        vacancy.RoleRequirementId,
                        coveragePercent,
                        readinessScore,
                        band,
                        gapCount,
                        criticalGapCount));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
