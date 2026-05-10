using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using MassTransit;

namespace Karamchari.HR.Services;

internal sealed class EmployeeService(
    HRDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ITenantProvider tenantProvider) : IEmployeeService
{
    /// <summary>
    /// Onboards a new employee and publishes the integration event for downstream consumers.
    /// </summary>
    /// <remarks>
    /// Outbox ordering: IPublishEndpoint.Publish is called BEFORE SaveChangesAsync intentionally.
    /// MassTransit writes OutboxMessage rows inside the same EF Core transaction as the Employee
    /// insert. If SaveChangesAsync throws, neither row is committed â€” the event is never delivered.
    ///
    /// Employee.Hire also raises EmployeeHired domain event, which DomainEventDispatchInterceptor
    /// dispatches during SaveChangesAsync. Both events are committed atomically.
    /// </remarks>
    public async Task<Guid> OnboardEmployeeAsync(OnboardEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        var employee = Employee.Hire(
            command.EmployeeNumber,
            command.LegalName,
            command.WorkEmail,
            command.HiredOn);

        dbContext.Employees.Add(employee);

        await publishEndpoint.Publish(new EmployeeOnboardedIntegrationEvent(
            employee.Id,
            tenantId,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
