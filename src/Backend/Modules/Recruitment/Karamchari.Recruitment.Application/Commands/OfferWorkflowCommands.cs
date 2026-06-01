using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record ApproveOfferCommand(OfferId OfferId) : IRequest;
public record IssueOfferCommand(OfferId OfferId, DateTimeOffset ExpiresAt) : IRequest;

public sealed class OfferWorkflowHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<ApproveOfferCommand>, IRequestHandler<IssueOfferCommand>
{
    public async Task Handle(ApproveOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer not found.");

        offer.SubmitForApproval(); // Draft -> Pending
        offer.Approve();           // Pending -> Approved

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = offer.Id.Value,
            EntityType = "Offer",
            Action = "OfferApproved",
            NewState = JsonSerializer.Serialize(offer),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(IssueOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer not found.");

        offer.Issue(request.ExpiresAt);
        
        var application = await dbContext.Applications.FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, cancellationToken);
        application?.MarkAsOffered();

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = offer.Id.Value,
            EntityType = "Offer",
            Action = "OfferIssued",
            NewState = JsonSerializer.Serialize(offer),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
