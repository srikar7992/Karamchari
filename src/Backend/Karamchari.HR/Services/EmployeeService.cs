using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using MassTransit;

namespace Karamchari.HR.Services;

internal sealed class EmployeeService(
    HRDbContext dbContext,
    IPublishEndpoint publishEndpoint) : IEmployeeService
{
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public async Task<Guid> OnboardEmployeeAsync(OnboardEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var employee = Employee.Hire(
            command.EmployeeNumber,
            command.LegalName,
            command.WorkEmail,
            command.HiredOn);

        dbContext.Employees.Add(employee);

        await publishEndpoint.Publish(new EmployeeOnboardedIntegrationEvent(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
