// -----------------------------------------------------------------------
// <copyright file="EmployeeCompensationProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Compensation.Contracts.IntegrationEvents;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that updates the cached employee compensation read model 
/// in response to compensation revision events.
/// </summary>
public sealed class EmployeeCompensationProjectionHandler
    : IProjectionHandler<EmployeeCompensationRevisedIntegrationEvent>
{
    private readonly HRDbContext _dbContext;

    public EmployeeCompensationProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the compensation revised event by updating the corresponding read model projection in-place.
    /// </summary>
    public async Task HandleAsync(EmployeeCompensationRevisedIntegrationEvent domainEvent, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EmployeeCompProjections
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.EmployeeId == domainEvent.EmployeeId, cancellationToken);

        if (existing != null)
        {
            existing.AnnualCTC = domainEvent.NewAnnualCTC;
            existing.PreviousAnnualCTC = domainEvent.PreviousAnnualCTC;
            existing.IncrementPercent = domainEvent.IncrementPercent;
            existing.EffectiveDate = domainEvent.EffectiveDate;
            existing.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;
        }
        else
        {
            var projection = new EmployeeCompensationProjection
            {
                TenantId = domainEvent.TenantId,
                EmployeeId = domainEvent.EmployeeId,
                AnnualCTC = domainEvent.NewAnnualCTC,
                PreviousAnnualCTC = domainEvent.PreviousAnnualCTC,
                IncrementPercent = domainEvent.IncrementPercent,
                EffectiveDate = domainEvent.EffectiveDate,
                LastUpdatedAtUtc = domainEvent.OccurredOnUtc
            };
            _dbContext.EmployeeCompProjections.Add(projection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

