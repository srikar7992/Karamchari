// -----------------------------------------------------------------------
// <copyright file="GetPipelineQuery.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Contracts;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

/// <summary>
/// Returns a grouped projection of all live applications across the recruitment pipeline
/// stages (New, Screening, Interviewing, Offered, Hired). Terminal/withdrawn applications
/// are excluded from the live pipeline view.
/// </summary>
public record GetPipelineQuery() : IRequest<PipelineDto>;

public sealed class GetPipelineHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<GetPipelineQuery, PipelineDto>
{
    private static readonly ApplicationStatus[] LiveStages =
    {
        ApplicationStatus.New,
        ApplicationStatus.Screening,
        ApplicationStatus.Interviewing,
        ApplicationStatus.Offered,
        ApplicationStatus.Hired,
    };

    public async Task<PipelineDto> Handle(
        GetPipelineQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Applications
            .AsNoTracking()
            .Where(a => LiveStages.Contains(a.Status))
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new
            {
                ApplicationId = a.Id.Value,
                a.CandidateId,
                a.RequisitionId,
                a.AppliedAt,
                Stage = a.Status.ToString(),
                Snapshot = a.CandidateSnapshot,
            })
            .ToListAsync(cancellationToken);

        var stageOrder = LiveStages.Select(s => s.ToString()).ToList();
        var stageCounts = stageOrder.ToDictionary(stage => stage, stage => 0);
        var groupedStages = new List<PipelineStageDto>();

        foreach (var stage in stageOrder)
        {
            var cards = rows
                .Where(r => r.Stage == stage)
                .Select(r => new PipelineCandidateDto(
                    r.ApplicationId,
                    r.CandidateId.Value,
                    r.Snapshot is { } snap ? $"{snap.FirstName} {snap.LastName}" : r.CandidateId.Value.ToString(),
                    r.Snapshot?.Email ?? string.Empty,
                    r.Snapshot?.PhoneNumber,
                    r.RequisitionId.Value,
                    r.AppliedAt,
                    stage))
                .ToList();

            stageCounts[stage] = cards.Count;
            groupedStages.Add(new PipelineStageDto(stage, cards));
        }

        return new PipelineDto(groupedStages, stageCounts);
    }
}
