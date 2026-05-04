using Karamchari.HR.Contracts.Organization;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Persistence;

namespace Karamchari.HR.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly HRDbContext _dbContext;

    public OrganizationService(HRDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateDepartmentAsync(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var department = Department.Create(command.Name, command.Description);

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return department.Id;
    }
}
