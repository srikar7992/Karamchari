// -----------------------------------------------------------------------
// <copyright file="SpanOfControlProjectionHandler.cs" company="Karamchari">
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
/// Projection handler that manages span of control hierarchy tracking, including
/// manager-direct report connections.
/// </summary>
public sealed class SpanOfControlProjectionHandler
    : IProjectionHandler<ReportingRelationshipCreated>,
      IProjectionHandler<ReportingRelationshipEnded>
{
    private readonly HRDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanOfControlProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    public SpanOfControlProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ReportingRelationshipCreated domainEvent, CancellationToken cancellationToken)
    {
        // 1. Fetch direct report occupant name if assigned
        var occupant = await _dbContext.PositionOccupantProjections.AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.PositionId == domainEvent.DirectReportPositionId, cancellationToken);

        // 2. Create child record
        var directReport = new ManagerDirectReportProjection
        {
            TenantId = domainEvent.TenantId,
            ManagerPositionId = domainEvent.ManagerPositionId,
            DirectReportPositionId = domainEvent.DirectReportPositionId,
            DirectReportEmployeeId = occupant?.EmployeeId,
            DirectReportEmployeeName = occupant?.EmployeeName
        };
        _dbContext.ManagerDirectReportProjections.Add(directReport);

        // 3. Update Span of Control parent record
        var span = await _dbContext.SpanOfControlProjections
            .FirstOrDefaultAsync(s => s.TenantId == domainEvent.TenantId && s.ManagerPositionId == domainEvent.ManagerPositionId, cancellationToken);

        if (span is null)
        {
            var managerOccupant = await _dbContext.PositionOccupantProjections.AsNoTracking()
                .FirstOrDefaultAsync(o => o.TenantId == domainEvent.TenantId && o.PositionId == domainEvent.ManagerPositionId, cancellationToken);

            span = new SpanOfControlProjection
            {
                TenantId = domainEvent.TenantId,
                ManagerPositionId = domainEvent.ManagerPositionId,
                ManagerEmployeeId = managerOccupant?.EmployeeId,
                ManagerName = managerOccupant?.EmployeeName ?? "Vacant Manager",
                DirectReportCount = 0,
                IndirectReportCount = 0,
                LastUpdatedAtUtc = domainEvent.OccurredOnUtc
            };
            _dbContext.SpanOfControlProjections.Add(span);
        }

        span.DirectReportCount += 1;
        span.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(ReportingRelationshipEnded domainEvent, CancellationToken cancellationToken)
    {
        // 1. Remove child record
        var directReport = await _dbContext.ManagerDirectReportProjections
            .FirstOrDefaultAsync(d => d.TenantId == domainEvent.TenantId && d.ManagerPositionId == domainEvent.ManagerPositionId && d.DirectReportPositionId == domainEvent.DirectReportPositionId, cancellationToken);
        if (directReport is not null)
        {
            _dbContext.ManagerDirectReportProjections.Remove(directReport);
        }

        // 2. Update Span of Control parent record
        var span = await _dbContext.SpanOfControlProjections
            .FirstOrDefaultAsync(s => s.TenantId == domainEvent.TenantId && s.ManagerPositionId == domainEvent.ManagerPositionId, cancellationToken);

        if (span is not null)
        {
            span.DirectReportCount = Math.Max(0, span.DirectReportCount - 1);
            span.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
