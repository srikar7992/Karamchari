// -----------------------------------------------------------------------
// <copyright file="VacancyPipelineProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Projections;

/// <summary>
/// Projection handler that manages vacancy pipeline tracking read-models.
/// </summary>
public sealed class VacancyPipelineProjectionHandler
    : IProjectionHandler<PositionVacancyOpened>,
      IProjectionHandler<PositionVacancyClosed>
{
    private readonly HRDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="VacancyPipelineProjectionHandler"/> class.
    /// </summary>
    public VacancyPipelineProjectionHandler(HRDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionVacancyOpened domainEvent, CancellationToken cancellationToken)
    {
        // Fetch position code
        var position = await _dbContext.Positions.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == domainEvent.TenantId && p.Id == domainEvent.PositionId, cancellationToken);
        var positionCode = position?.Code ?? $"POS-{domainEvent.PositionId}";

        var projection = new VacancyPipelineProjection
        {
            TenantId = domainEvent.TenantId,
            VacancyId = domainEvent.VacancyId,
            PositionId = domainEvent.PositionId,
            PositionCode = positionCode,
            OpenedUtc = domainEvent.OpenedUtc,
            Status = domainEvent.Status.ToString(),
            Reason = domainEvent.Reason,
            RecruitmentRequisitionId = domainEvent.RecruitmentRequisitionId,
            ExternalReferenceId = domainEvent.ExternalReferenceId,
            ClosedUtc = null,
            ClosedReason = null,
            FilledByEmployeeId = null,
            FilledPositionAssignmentId = null,
            LastUpdatedAtUtc = domainEvent.OccurredOnUtc
        };

        _dbContext.VacancyPipelineProjections.Add(projection);

        // Publish integration event so Capability module can generate InternalMobilityProjection rows.
        // position already fetched above — reuse it. Published before SaveChanges so outbox commits atomically.
        await _publishEndpoint.Publish(new VacancyOpenedIntegrationEvent(
            domainEvent.VacancyId,
            domainEvent.TenantId,
            domainEvent.PositionId,
            position?.RoleSkillRequirementId ?? domainEvent.RoleSkillRequirementId,
            domainEvent.OpenedUtc), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleAsync(PositionVacancyClosed domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _dbContext.VacancyPipelineProjections
            .FirstOrDefaultAsync(v => v.TenantId == domainEvent.TenantId && v.VacancyId == domainEvent.VacancyId, cancellationToken);

        if (projection is not null)
        {
            projection.Status = domainEvent.Status.ToString();
            projection.ClosedUtc = domainEvent.ClosedUtc;
            projection.ClosedReason = domainEvent.ClosedReason;
            projection.FilledByEmployeeId = domainEvent.FilledByEmployeeId;
            projection.FilledPositionAssignmentId = domainEvent.FilledPositionAssignmentId;
            projection.LastUpdatedAtUtc = domainEvent.OccurredOnUtc;

            await _publishEndpoint.Publish(new VacancyClosedIntegrationEvent(
                domainEvent.VacancyId,
                domainEvent.TenantId,
                domainEvent.PositionId,
                domainEvent.Status.ToString()), cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
