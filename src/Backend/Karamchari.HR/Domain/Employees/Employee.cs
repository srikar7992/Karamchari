using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Employees.Events;

namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// HR aggregate root for a person employed by a tenant organization.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>, ITenantOwned
{
    private Employee(
        Guid id,
        string employeeNumber,
        string legalName,
        string? workEmail,
        DateOnly hiredOn)
        : base(id)
    {
        EmployeeNumber = NormalizeRequired(employeeNumber, nameof(employeeNumber));
        LegalName = NormalizeRequired(legalName, nameof(legalName));
        WorkEmail = NormalizeOptional(workEmail);
        HiredOn = hiredOn;
        Status = EmploymentStatus.Active;
    }

    private Employee()
    {
        TenantId = string.Empty;
        EmployeeNumber = string.Empty;
        LegalName = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;

    public string EmployeeNumber { get; private set; }

    public string LegalName { get; private set; }

    public string? WorkEmail { get; private set; }

    public DateOnly HiredOn { get; private set; }

    public EmploymentStatus Status { get; private set; }

    public static Employee Hire(
        string employeeNumber,
        string legalName,
        string? workEmail,
        DateOnly hiredOn)
    {
        var employee = new Employee(Guid.NewGuid(), employeeNumber, legalName, workEmail, hiredOn);
        employee.RaiseDomainEvent(new EmployeeHired(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn));
        return employee;
    }

    public void Rename(string legalName) => LegalName = NormalizeRequired(legalName, nameof(legalName));

    public void ChangeWorkEmail(string? workEmail) => WorkEmail = NormalizeOptional(workEmail);

    public void Terminate()
    {
        if (Status == EmploymentStatus.Terminated)
        {
            return;
        }

        Status = EmploymentStatus.Terminated;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
