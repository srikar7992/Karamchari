using System.Text.Json;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record PublishRequisitionCommand(RequisitionId RequisitionId) : IRequest;

public sealed class PublishRequisitionHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<PublishRequisitionCommand>
{
    public async Task Handle(PublishRequisitionCommand request, CancellationToken cancellationToken)
    {
        var requisition = await dbContext.Requisitions
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Requisition {request.RequisitionId.Value} not found.");

        if (requisition.Status == RequisitionStatus.Draft)
        {
            requisition.SubmitForApproval();
        }

        if (requisition.Status == RequisitionStatus.PendingApproval)
        {
            requisition.Approve(); // Transitions state to Published and raises domain event
        }
        else if (requisition.Status != RequisitionStatus.Published)
        {
            throw new InvalidOperationException($"Cannot publish requisition in status {requisition.Status}.");
        }

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = requisition.Id.Value,
            EntityType = "Requisition",
            Action = "Published",
            NewState = JsonSerializer.Serialize(requisition),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
