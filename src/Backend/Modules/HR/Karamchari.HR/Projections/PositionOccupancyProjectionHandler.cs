// -----------------------------------------------------------------------
// <copyright file="PositionOccupancyProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that manages position occupancy rates, occupant tracking,
/// and department headcount metrics.
/// </summary>
public sealed class PositionOccupancyProjectionHandler
    : IProjectionHandler<PositionCreated>,
      IProjectionHandler<PositionStatusChanged>,
      IProjectionHandler<PositionAssignmentCreated>,
      IProjectionHandler<PositionAssignmentEnded>
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionOccupancyProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    public PositionOccupancyProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionCreated domainEvent, CancellationToken cancellationToken)
    {
        // Fetch Designation Name
        var designation = await _dbContext.Designations.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == domainEvent.TenantId && d.Id == domainEvent.DesignationId, cancellationToken);
        var designationName = designation?.Name ?? $"Designation-{domainEvent.DesignationId}";

        var projection = new PositionOccupancyProjection
        {
            TenantId = domainEvent.TenantId,
            PositionId = domainEvent.PositionId,
            Code = domainEvent.Code,
            OrgUnitId = domainEvent.OrgUnitId,
            DesignationId = domainEvent.DesignationId,
            DesignationName = designationName,
            Status = domainEvent.Status.ToString(),
            AllowMultipleOccupants = domainEvent.AllowMultipleOccupants,
            ApprovedHeadcount = domainEvent.ApprovedHeadcount,
            CurrentOccupantCount = 0,
            VacancyCount = domainEvent.ApprovedHeadcount,
            OccupancyRate = 0.0,
            LastUpdatedAtUtc = domainEvent.OccurredOnUtc
        };

        _dbContext.PositionOccupancyProjections.Add(projection);

        // Update Org unit metrics
        if (domainEvent.OrgUnitId.HasValue)
        {
            var metrics = await _dbContext.OrgMetricsProjections
                .FirstOrDefaultAsync(m => m.TenantId == domainEvent.TenantId && m.OrganizationUnitId == domainEvent.OrgUnitId.Value, cancellationToken);
            if (metrics is not null)
            {
                metrics.ActivePositionCount += 1;
                metrics.ApprovedHeadcount += domainEvent.ApprovedHeadcount;
                metrics.OpenVacancyCount += domainEvent.ApprovedHeadcount;
                metrics.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionStatusChanged domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.PositionOccupancyProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (projection is not null)
        {
            projection.Status = domainEvent.Status.ToString();
            projection.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionAssignmentCreated domainEvent, CancellationToken cancellationToken)
    {
        // 1. Fetch employee name
        var employee = await _dbContext.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == domainEvent.TenantId && e.Id == domainEvent.EmployeeId, cancellationToken);
        var employeeName = employee?.LegalName ?? $"Employee {domainEvent.EmployeeId}";

        // 2. Create occupant projection record
        var occupant = new PositionOccupantProjection
        {
            TenantId = domainEvent.TenantId,
            PositionId = domainEvent.PositionId,
            EmployeeId = domainEvent.EmployeeId,
            AssignmentId = domainEvent.AssignmentId,
            EmployeeName = employeeName
        };
        _dbContext.PositionOccupantProjections.Add(occupant);

        // 3. Update PositionOccupancyProjection
        var position = await _dbContext.PositionOccupancyProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (position is not null)
        {
            position.CurrentOccupantCount += 1;
            position.VacancyCount = Math.Max(0, position.ApprovedHeadcount - position.CurrentOccupantCount);
            position.OccupancyRate = position.ApprovedHeadcount > 0
                ? (double)position.CurrentOccupantCount / position.ApprovedHeadcount
                : 0.0;
            position.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;

            // 4. Update Org unit metrics
            if (position.OrgUnitId.HasValue)
            {
                var metrics = await _dbContext.OrgMetricsProjections
                    .FirstOrDefaultAsync(m => m.TenantId == domainEvent.TenantId && m.OrganizationUnitId == position.OrgUnitId.Value, cancellationToken);
                if (metrics is not null)
                {
                    metrics.CurrentHeadcount += 1;
                    metrics.OpenVacancyCount = Math.Max(0, metrics.OpenVacancyCount - 1);
                    metrics.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionAssignmentEnded domainEvent, CancellationToken cancellationToken)
    {
        // 1. Remove occupant projection record
        var occupant = await _dbContext.PositionOccupantProjections
            .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.AssignmentId == domainEvent.AssignmentId, cancellationToken);
        if (occupant is not null)
        {
            _dbContext.PositionOccupantProjections.Remove(occupant);
        }

        // 2. Update PositionOccupancyProjection
        var position = await _dbContext.PositionOccupancyProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.PositionId == domainEvent.PositionId, cancellationToken);

        if (position is not null)
        {
            position.CurrentOccupantCount = Math.Max(0, position.CurrentOccupantCount - 1);
            position.VacancyCount = Math.Max(0, position.ApprovedHeadcount - position.CurrentOccupantCount);
            position.OccupancyRate = position.ApprovedHeadcount > 0
                ? (double)position.CurrentOccupantCount / position.ApprovedHeadcount
                : 0.0;
            position.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;

            // 3. Update Org unit metrics
            if (position.OrgUnitId.HasValue)
            {
                var metrics = await _dbContext.OrgMetricsProjections
                    .FirstOrDefaultAsync(m => m.TenantId == domainEvent.TenantId && m.OrganizationUnitId == position.OrgUnitId.Value, cancellationToken);
                if (metrics is not null)
                {
                    metrics.CurrentHeadcount = Math.Max(0, metrics.CurrentHeadcount - 1);
                    metrics.OpenVacancyCount += 1;
                    metrics.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
