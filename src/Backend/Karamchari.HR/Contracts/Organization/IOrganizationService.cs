namespace Karamchari.HR.Contracts.Organization;

public interface IOrganizationService
{
    Task<Guid> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default);
}
