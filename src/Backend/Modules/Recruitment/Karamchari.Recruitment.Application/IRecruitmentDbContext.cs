// -----------------------------------------------------------------------
// <copyright file="IRecruitmentDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Application.Analytics;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application;

public interface IRecruitmentDbContext
{
    DbSet<JobRequisition> Requisitions { get; }
    DbSet<Candidate> Candidates { get; }
    DbSet<Karamchari.Recruitment.Domain.Application> Applications { get; }
    DbSet<Interview> Interviews { get; }
    DbSet<InterviewFeedback> InterviewFeedbacks { get; }
    DbSet<Offer> Offers { get; }
    DbSet<RecruitmentAuditEntry> AuditStream { get; }
    DbSet<AnalyticsReadModel> AnalyticsReadModels { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
