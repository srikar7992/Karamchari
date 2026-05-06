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

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique employee number.
    /// </summary>
    public string EmployeeNumber { get; private set; }

    /// <summary>
    /// Gets the legal name of the employee.
    /// </summary>
    public string LegalName { get; private set; }

    /// <summary>
    /// Gets the work email address of the employee.
    /// </summary>
    public string? WorkEmail { get; private set; }

    /// <summary>
    /// Gets the date the employee was hired.
    /// </summary>
    public DateOnly HiredOn { get; private set; }

    /// <summary>
    /// Gets the current employment status.
    /// </summary>
    public EmploymentStatus Status { get; private set; }

    /// <summary>
    /// Gets the identifier for the regional holiday calendar.
    /// </summary>
    public Guid? HolidayCalendarId { get; private set; }

    /// <summary>
    /// Gets the employee's primary work time zone (IANA format).
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    /// <summary>
    /// Hires a new employee.
    /// </summary>
    public static Employee Hire(
        string employeeNumber,
        string legalName,
        string? workEmail,
        DateOnly hiredOn,
        Guid? holidayCalendarId = null,
        string timeZoneId = "UTC")
    {
        var employee = new Employee(Guid.NewGuid(), employeeNumber, legalName, workEmail, hiredOn)
        {
            HolidayCalendarId = holidayCalendarId,
            TimeZoneId = timeZoneId
        };
        employee.RaiseDomainEvent(new EmployeeHired(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.HiredOn));
        return employee;
    }

    /// <summary>
    /// Renames the employee.
    /// </summary>
    /// <param name="legalName">The new legal name.</param>
    public void Rename(string legalName) => LegalName = NormalizeRequired(legalName, nameof(legalName));

    /// <summary>
    /// Changes the employee's work email.
    /// </summary>
    /// <param name="workEmail">The new work email.</param>
    public void ChangeWorkEmail(string? workEmail) => WorkEmail = NormalizeOptional(workEmail);

    /// <summary>
    /// Terminates the employee's employment.
    /// </summary>
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
