// -----------------------------------------------------------------------
// <copyright file="EmployeePerformanceProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Karamchari.Performance.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that updates the cached employee performance read model 
/// in response to performance review snapshot events.
/// </summary>
public sealed class EmployeePerformanceProjectionHandler
    : IProjectionHandler<EmployeePerformanceSnapshotMaterializedIntegrationEventV1>
{
    private readonly HRDbContext _dbContext;

    public EmployeePerformanceProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the performance snapshot materialized event by updating the corresponding read model projection in-place.
    /// </summary>
    public async Task HandleAsync(EmployeePerformanceSnapshotMaterializedIntegrationEventV1 domainEvent, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EmployeePerfProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.EmployeeId == domainEvent.EmployeeId, cancellationToken);

        if (existing != null)
        {
            existing.CompositeScore = domainEvent.CompositeScore;
            existing.PerformanceBucket = domainEvent.PerformanceBucket;
            existing.IsHighPerformer = domainEvent.IsHighPerformer;
            existing.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
        }
        else
        {
            var projection = new EmployeePerformanceProjection
            {
                TenantId = domainEvent.TenantId,
                EmployeeId = domainEvent.EmployeeId,
                CompositeScore = domainEvent.CompositeScore,
                PerformanceBucket = domainEvent.PerformanceBucket,
                IsHighPerformer = domainEvent.IsHighPerformer,
                LastUpdatedAtUtc = domainEvent.OccurredOnUtc
            };
            _dbContext.EmployeePerfProjections.Add(projection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

