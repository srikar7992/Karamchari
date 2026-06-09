using System.Threading;
using System.Threading.Tasks;
using Karamchari.Core.Projections;
using Karamchari.HR.Domain.Organization.Events;
using Karamchari.HR.Domain.Organization.Projections;
using Karamchari.HR.Persistence;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="VacancyPipelineProjectionHandler"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for HR module persistence.</param>
    public VacancyPipelineProjectionHandler(HRDbContext dbContext)
    {
        _dbContext = dbContext;
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

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
