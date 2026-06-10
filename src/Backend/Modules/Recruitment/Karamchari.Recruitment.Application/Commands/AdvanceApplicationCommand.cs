// -----------------------------------------------------------------------
// <copyright file="AdvanceApplicationCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record AdvanceApplicationCommand(Karamchari.Recruitment.Domain.ApplicationId ApplicationId) : IRequest;

public sealed class AdvanceApplicationHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<AdvanceApplicationCommand>
{
    public async Task Handle(AdvanceApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (application.Status == ApplicationStatus.New)
        {
            application.AdvanceToScreening();
        }
        else if (application.Status == ApplicationStatus.Screening)
        {
            application.AdvanceToInterviewing();
        }
        else
        {
            throw new InvalidOperationException($"Cannot advance application in status {application.Status}.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
