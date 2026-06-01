using System.Text.Json;
using Karamchari.Recruitment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Commands;

public record HireCandidateCommand(Karamchari.Recruitment.Domain.ApplicationId ApplicationId, string HiredBy) : IRequest;

public sealed class HireCandidateHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<HireCandidateCommand>
{
    public async Task Handle(HireCandidateCommand request, CancellationToken cancellationToken)
    {
        var application = await dbContext.Applications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");
        
        // 1. Transition state
        application.Hire(request.HiredBy);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = application.Id.Value,
            EntityType = "Application",
            Action = "Hired",
            NewState = JsonSerializer.Serialize(application),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);
        
        // 2. Persist (triggers domain event -> integration event handler)
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
