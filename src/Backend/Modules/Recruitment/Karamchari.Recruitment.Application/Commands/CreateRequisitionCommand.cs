// -----------------------------------------------------------------------
// <copyright file="CreateRequisitionCommand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.Recruitment.Application;
using Karamchari.Recruitment.Domain;
using MediatR;

namespace Karamchari.Recruitment.Application.Commands;

public record CreateRequisitionCommand(
    string Title,
    Guid DepartmentId,
    Guid HiringManagerId) : IRequest<RequisitionId>;

public sealed class CreateRequisitionHandler(IRecruitmentDbContext dbContext)
    : IRequestHandler<CreateRequisitionCommand, RequisitionId>
{
    public async Task<RequisitionId> Handle(CreateRequisitionCommand request, CancellationToken cancellationToken)
    {
        var requisition = JobRequisition.Create(request.Title, request.DepartmentId, request.HiringManagerId);

        dbContext.Requisitions.Add(requisition);

        var auditEntry = new RecruitmentAuditEntry
        {
            Id = Guid.NewGuid(),
            EntityId = requisition.Id.Value,
            EntityType = "Requisition",
            Action = "Created",
            NewState = JsonSerializer.Serialize(requisition),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = "system"
        };
        dbContext.AuditStream.Add(auditEntry);

        await dbContext.SaveChangesAsync(cancellationToken);

        return requisition.Id;
    }
}
