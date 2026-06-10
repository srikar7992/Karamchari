// -----------------------------------------------------------------------
// <copyright file="CreateCandidateCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Domain;
using MediatR;

namespace Karamchari.Recruitment.Application.Commands;

public record CreateCandidateCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber) : IRequest<CandidateId>;

public sealed class CreateCandidateHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<CreateCandidateCommand, CandidateId>
{
    public async Task<CandidateId> Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
    {
        var candidate = Candidate.Create(request.FirstName, request.LastName, request.Email);
        if (request.PhoneNumber != null)
        {
            candidate.UpdateProfile(request.FirstName, request.LastName, request.Email, request.PhoneNumber);
        }

        dbContext.Candidates.Add(candidate);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = candidate.Id.Value,
            EntityType = "Candidate",
            Action = "Created",
            NewState = JsonSerializer.Serialize(candidate),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return candidate.Id;
    }
}
