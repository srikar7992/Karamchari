using Karamchari.Recruitment.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Recruitment.Application.Queries;

public record GetRequisitionQuery(Guid Id) : IRequest<Karamchari.Recruitment.Contracts.RequisitionDto?>;

public sealed class GetRequisitionHandler(IRecruitmentDbContext dbContext) 
    : IRequestHandler<GetRequisitionQuery, Karamchari.Recruitment.Contracts.RequisitionDto?>
{
    public async Task<Karamchari.Recruitment.Contracts.RequisitionDto?> Handle(GetRequisitionQuery request, CancellationToken cancellationToken)
    {
        var req = await dbContext.Requisitions
            .FirstOrDefaultAsync(r => r.Id == new Domain.RequisitionId(request.Id), cancellationToken);
            
        if (req == null) return null;

        return new Karamchari.Recruitment.Contracts.RequisitionDto(
            req.Id.Value,
            req.Title,
            req.DepartmentId.ToString(),
            req.HiringManagerId,
            req.Status.ToString(),
            req.TargetHireDate);
    }
}
