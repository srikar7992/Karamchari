namespace Karamchari.Payroll.Services.Declarations;

using Karamchari.Payroll.Domain.Statutory;
using Karamchari.Payroll.Services.Statutory;

/// <summary>
/// Service for managing the tax declaration lifecycle.
/// </summary>
public class ITDeclarationService
{
    private readonly IITDeclarationRepository _repo;

    public ITDeclarationService(IITDeclarationRepository repo)
    {
        _repo = repo;
    }

    public async Task ApproveAsync(Guid id, decimal approvedAmount, string approver)
    {
        var declaration = await _repo.GetByIdAsync(id) 
            ?? throw new InvalidOperationException("Declaration not found.");

        declaration.Approve(approvedAmount, approver);

        await _repo.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid id, string reason)
    {
        var declaration = await _repo.GetByIdAsync(id) 
            ?? throw new InvalidOperationException("Declaration not found.");

        declaration.Reject(reason);

        await _repo.SaveChangesAsync();
    }
}
