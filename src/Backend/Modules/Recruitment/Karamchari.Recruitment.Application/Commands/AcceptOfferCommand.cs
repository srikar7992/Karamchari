using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record AcceptOfferCommand(OfferId OfferId) : IRequest;

public sealed class AcceptOfferHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<AcceptOfferCommand>
{
    public async Task Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Offers.FirstOrDefaultAsync(o => o.Id == request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer not found.");

        offer.Accept();

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = offer.Id.Value,
            EntityType = "Offer",
            Action = "OfferAccepted",
            NewState = JsonSerializer.Serialize(offer),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
