using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record CreateOfferCommand(
    Karamchari.Recruitment.Domain.ApplicationId ApplicationId,
    decimal BaseSalary,
    string Currency) : IRequest<OfferId>;

public sealed class CreateOfferHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<CreateOfferCommand, OfferId>
{
    public async Task<OfferId> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {request.ApplicationId.Value} not found.");
        
        if (application.Status != ApplicationStatus.Interviewing)
            throw new InvalidOperationException("Can only draft offers for applications in interviewing stage.");

        var offer = Offer.Create(request.ApplicationId, request.BaseSalary, request.Currency);
        
        dbContext.Offers.Add(offer);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = offer.Id.Value,
            EntityType = "Offer",
            Action = "OfferDrafted",
            NewState = JsonSerializer.Serialize(offer),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);
        
        return offer.Id;
    }
}
