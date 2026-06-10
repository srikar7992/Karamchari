// -----------------------------------------------------------------------
// <copyright file="OrganizationHierarchyProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that updates organizational hierarchy read-models and metrics
/// in response to unit creation and deactivation events.
/// </summary>
public sealed class OrganizationHierarchyProjectionHandler
    : IProjectionHandler<OrganizationUnitCreated>,
      IProjectionHandler<OrganizationUnitDeactivated>
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationHierarchyProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    public OrganizationHierarchyProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(OrganizationUnitCreated domainEvent, CancellationToken cancellationToken)
    {
        var path = $"/{domainEvent.Code}";
        var level = 0;

        if (domainEvent.ParentUnitId.HasValue)
        {
            var parent = await _dbContext.OrgHierarchyProjections
                .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.OrganizationUnitId == domainEvent.ParentUnitId.Value, cancellationToken);
            if (parent is not null)
            {
                path = $"{parent.HierarchyPath}/{domainEvent.Code}";
                level = parent.Level + 1;
            }
        }

        var projection = new OrganizationHierarchyProjection
        {
            TenantId = domainEvent.TenantId,
            OrganizationUnitId = domainEvent.UnitId,
            ParentUnitId = domainEvent.ParentUnitId,
            Name = domainEvent.Name,
            Code = domainEvent.Code,
            Type = domainEvent.Type.ToString(),
            HierarchyPath = path,
            Level = level,
            LastUpdatedAtUtc = domainEvent.OccurredOnUtc
        };

        _dbContext.OrgHierarchyProjections.Add(projection);

        // Also seed default metrics record for the new org unit
        var metrics = new OrganizationMetricsProjection
        {
            TenantId = domainEvent.TenantId,
            OrganizationUnitId = domainEvent.UnitId,
            LeadEmployeeId = domainEvent.LeadEmployeeId,
            LeadEmployeeName = null, // To be loaded if needed
            ActivePositionCount = 0,
            ApprovedHeadcount = 0,
            CurrentHeadcount = 0,
            OpenVacancyCount = 0,
            LastUpdatedAtUtc = domainEvent.OccurredOnUtc
        };
        _dbContext.OrgMetricsProjections.Add(metrics);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(OrganizationUnitDeactivated domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.OrgHierarchyProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.OrganizationUnitId == domainEvent.UnitId, cancellationToken);
        if (projection is not null)
        {
            _dbContext.OrgHierarchyProjections.Remove(projection);
        }

        var metrics = await _dbContext.OrgMetricsProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.OrganizationUnitId == domainEvent.UnitId, cancellationToken);
        if (metrics is not null)
        {
            _dbContext.OrgMetricsProjections.Remove(metrics);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
