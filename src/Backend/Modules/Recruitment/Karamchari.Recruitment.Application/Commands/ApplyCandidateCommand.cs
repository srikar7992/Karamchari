using System.Text.Json;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record ApplyCandidateCommand(
    CandidateId CandidateId,
    RequisitionId RequisitionId) : IRequest<Karamchari.Recruitment.Domain.ApplicationId>;

public sealed class ApplyCandidateHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<ApplyCandidateCommand, Karamchari.Recruitment.Domain.ApplicationId>
{
    public async Task<Karamchari.Recruitment.Domain.ApplicationId> Handle(ApplyCandidateCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Candidate and Requisition exist
        var candidate = await dbContext.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken)
            ?? throw new InvalidOperationException($"Candidate {request.CandidateId.Value} not found.");

        var requisition = await dbContext.Requisitions
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Requisition {request.RequisitionId.Value} not found.");

        if (requisition.Status != RequisitionStatus.Published)
            throw new InvalidOperationException("Cannot apply to a requisition that is not published.");

        // 2. IDEMPOTENCY: Check for existing application
        var existing = await dbContext.Applications
            .AnyAsync(a => a.CandidateId == request.CandidateId && a.RequisitionId == request.RequisitionId, cancellationToken);

        if (existing)
        {
            throw new InvalidOperationException("Candidate has already applied for this requisition.");
        }

        // 3. Create Application with point-in-time snapshot
        var snapshot = candidate.CreateSnapshot();
        var application = Karamchari.Recruitment.Domain.Application.Create(request.CandidateId, request.RequisitionId, snapshot);

        dbContext.Applications.Add(application);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = application.Id.Value,
            EntityType = "Application",
            Action = "Applied",
            NewState = JsonSerializer.Serialize(application),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return application.Id;
    }
}
