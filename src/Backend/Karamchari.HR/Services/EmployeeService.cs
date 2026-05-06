using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
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
    /// <summary>
    /// Onboards a new employee and publishes the integration event for downstream consumers.
    /// </summary>
    /// <param name="command">The onboard command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new employee identifier.</returns>
    /// <remarks>
    /// Outbox ordering: <c>IPublishEndpoint.Publish</c> is called BEFORE
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
    /// intentionally. With <c>AddEntityFrameworkOutbox&lt;HRDbContext&gt;</c> configured,
    /// MassTransit writes <c>OutboxMessage</c> rows inside the same EF Core transaction as
    /// the <c>Employee</c> insert. If <c>SaveChangesAsync</c> throws, neither the employee
    /// row nor the outbox rows are committed — the integration event is never delivered.
    /// This is the correct outbox pattern; the ordering looks backward but is right.
    ///
    /// Note: <c>Employee.Hire</c> also raises the <see cref="HR.Domain.Employees.Events.EmployeeHired"/>
    /// domain event, which <c>DomainEventDispatchInterceptor</c> dispatches during
    /// <c>SaveChangesAsync</c>. Both events are committed atomically.
    /// </remarks>
    public async Task<Guid> OnboardEmployeeAsync(OnboardEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var employee = Employee.Hire(
            command.EmployeeNumber,
            command.LegalName,
            command.WorkEmail,
            command.HiredOn);

        dbContext.Employees.Add(employee);

        // Publish the cross-context integration event. MassTransit's EF Core outbox
        // captures this in OutboxMessage within the same SaveChangesAsync transaction below.
        await publishEndpoint.Publish(new EmployeeOnboardedIntegrationEvent(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn), cancellationToken);

        // Commits: Employee row + OutboxMessage rows (both EmployeeOnboardedIntegrationEvent
        // and EmployeeHired from the domain event) in a single SQL transaction.
        await dbContext.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
