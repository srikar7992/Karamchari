// -----------------------------------------------------------------------
// <copyright file="SuccessionCoverageProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.Succession.Domain;
using Karamchari.Succession.Domain.Events;
using Karamchari.Succession.Domain.Projections;
using Karamchari.Succession.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Succession.Projections;

public sealed class SuccessionCoverageProjectionHandler
    : IProjectionHandler<PositionCreated>,
      IProjectionHandler<PositionAssignmentCreated>,
      IProjectionHandler<PositionAssignmentEnded>,
      IProjectionHandler<SuccessorAddedEvent>,
      IProjectionHandler<SuccessorPromotedEvent>
{
    private readonly SuccessionDbContext _dbContext;

    public SuccessionCoverageProjectionHandler(SuccessionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task HandleAsync(PositionCreated domainEvent, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SuccessionCoverageProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (existing is not null) return;

        var role = await _dbContext.CriticalRoles
            .FirstOrDefaultAsync(r => r.TenantId == domainEvent.TenantId && r.RoleTitle == domainEvent.Code && r.IsActive, cancellationToken);

        var isKey = role is not null;
        var risk = role?.Risk.ToString() ?? "Low";
        var successorCount = 0;
        var readyNowCount = 0;
        var nearTermCount = 0;

        if (role is not null)
        {
            var plan = await _dbContext.SuccessionPlans
                .Include(p => p.Candidates)
                .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.RoleId == role.Id, cancellationToken);

            if (plan is not null)
            {
                successorCount = plan.Candidates.Count(c => c.Status == SuccessorStatus.Active);
                readyNowCount = plan.Candidates.Count(c => c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.ReadyNow);
                nearTermCount = plan.Candidates.Count(c =>
                    c.Status == SuccessorStatus.Active &&
                    (c.Readiness == ReadinessLevel.ReadyWithinSixMonths ||
                     c.Readiness == ReadinessLevel.ReadyWithinTwelveMonths));
                risk = plan.ComputeRisk().ToString();
            }
        }

        var projection = new SuccessionCoverageProjection
        {
            TenantId = domainEvent.TenantId,
            PositionId = domainEvent.PositionId,
            PositionCode = domainEvent.Code,
            CurrentOccupantId = null,
            IsKeyPosition = isKey,
            SuccessorCount = successorCount,
            ReadyNowCount = readyNowCount,
            ReadyNearTermCount = nearTermCount,
            VacancyRisk = risk,
            LastUpdatedAtUtc = domainEvent.OccurredOnUtc,
        };

        _dbContext.SuccessionCoverageProjections.Add(projection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(PositionAssignmentCreated domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.SuccessionCoverageProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (projection is not null)
        {
            projection.CurrentOccupantId = domainEvent.EmployeeId;
            projection.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(PositionAssignmentEnded domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.SuccessionCoverageProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (projection is not null && projection.CurrentOccupantId == domainEvent.EmployeeId)
        {
            projection.CurrentOccupantId = null;
            projection.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleAsync(SuccessorAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateBenchStrengthAsync(domainEvent.TenantId, domainEvent.RoleId, domainEvent.OccurredOnUtc, cancellationToken);
    }

    public async Task HandleAsync(SuccessorPromotedEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateBenchStrengthAsync(domainEvent.TenantId, domainEvent.RoleId, domainEvent.OccurredOnUtc, cancellationToken);
    }

    private async Task UpdateBenchStrengthAsync(string tenantId, CriticalRoleId roleId, DateTimeOffset occurredOnUtc, CancellationToken cancellationToken)
    {
        var role = await _dbContext.CriticalRoles.FindAsync(new object[] { roleId }, cancellationToken);
        if (role is null || !role.IsActive) return;

        var plan = await _dbContext.SuccessionPlans
            .Include(p => p.Candidates)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.RoleId == roleId, cancellationToken);

        if (plan is null) return;

        var activeCandidates = plan.Candidates.Where(c => c.Status == SuccessorStatus.Active).ToList();
        var readyNowCount = activeCandidates.Count(c => c.Readiness == ReadinessLevel.ReadyNow);
        var nearTermCount = activeCandidates.Count(c =>
            c.Readiness == ReadinessLevel.ReadyWithinSixMonths ||
            c.Readiness == ReadinessLevel.ReadyWithinTwelveMonths);

        role.Recompute(readyNowCount, nearTermCount);

        var projections = await _dbContext.SuccessionCoverageProjections
            .Where(p => p.TenantId == tenantId && p.PositionCode == role.RoleTitle)
            .ToListAsync(cancellationToken);

        foreach (var proj in projections)
        {
            proj.IsKeyPosition = true;
            proj.SuccessorCount = activeCandidates.Count;
            proj.ReadyNowCount = readyNowCount;
            proj.ReadyNearTermCount = nearTermCount;
            proj.VacancyRisk = role.ComputeVacancyRisk().ToString();
            proj.LastUpdatedAtUtc = occurredOnUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
