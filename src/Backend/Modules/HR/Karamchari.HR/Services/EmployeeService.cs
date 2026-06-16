// -----------------------------------------------------------------------
// <copyright file="EmployeeService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

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

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (employee == null) return null;

        return new EmployeeDto(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn,
            employee.Status.ToString(),
            employee.TimeZoneId
        );
    }

    public async Task<EmployeeProfileDto?> GetEmployeeProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (employee == null) return null;

        string? departmentName = employee.DepartmentId is Guid deptId
            ? await dbContext.Departments.AsNoTracking().Where(d => d.Id == deptId).Select(d => d.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        string? designationName = null;
        int? designationLevel = null;
        if (employee.DesignationId is Guid desigId)
        {
            var designation = await dbContext.Designations.AsNoTracking()
                .Where(d => d.Id == desigId)
                .Select(d => new { d.Name, d.Level })
                .FirstOrDefaultAsync(cancellationToken);
            designationName = designation?.Name;
            designationLevel = designation?.Level;
        }

        string? branchName = employee.BranchId is Guid branchId
            ? await dbContext.Branches.AsNoTracking().Where(b => b.Id == branchId).Select(b => b.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        string? managerName = employee.ReportingManagerId is Guid managerId
            ? await dbContext.Employees.AsNoTracking().Where(m => m.Id == managerId).Select(m => m.LegalName).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new EmployeeProfileDto(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn,
            employee.Status.ToString(),
            employee.TimeZoneId,
            employee.EmploymentType.ToString(),
            departmentName,
            designationName,
            designationLevel,
            branchName,
            managerName);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return employees.Select(e => new EmployeeDto(
            e.Id,
            e.EmployeeNumber,
            e.LegalName,
            e.WorkEmail,
            e.HiredOn,
            e.Status.ToString(),
            e.TimeZoneId
        )).ToList();
    }

    public async Task UpdateEmployeeAsync(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

        employee.UpdateDetails(command.LegalName, command.WorkEmail);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var employee = await dbContext.Employees
            .Include(e => e.History)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"Employee with ID {id} was not found.");

        employee.Terminate(DateTimeOffset.UtcNow, Guid.Empty);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
