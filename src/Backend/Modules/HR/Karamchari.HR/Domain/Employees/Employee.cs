// -----------------------------------------------------------------------
// <copyright file="Employee.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Employees.Events;

namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// HR aggregate root for a person employed by a tenant organization.
/// Serves as the canonical workforce system-of-record.
/// Business truth is reconstructable via the immutable history ledger.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<EmployeeHistoryEntry> _history = [];

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
        TimeZoneId = "UTC";
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

    public Guid? DepartmentId { get; private set; }
    public Guid? DesignationId { get; private set; }
    public Guid? ReportingManagerId { get; private set; }
    public Guid? LegalEntityId { get; private set; }
    public Guid? BranchId { get; private set; }
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>
    /// Gets the identifier for the regional holiday calendar.
    /// </summary>
    public Guid? HolidayCalendarId { get; private set; }

    /// <summary>
    /// Gets the employee's primary work time zone (IANA format).
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    /// <summary>
    /// Historical timeline of organizational and lifecycle changes.
    /// </summary>
    public IReadOnlyCollection<EmployeeHistoryEntry> History => _history.AsReadOnly();

    /// <summary>
    /// Hires a new employee.
    /// </summary>
    public static Employee Hire(
        string employeeNumber,
        string legalName,
        string? workEmail,
        DateOnly hiredOn,
        EmploymentType employmentType = EmploymentType.FullTime,
        Guid? holidayCalendarId = null,
        string timeZoneId = "UTC")
    {
        var employee = new Employee(Guid.NewGuid(), employeeNumber, legalName, workEmail, hiredOn)
        {
            EmploymentType = employmentType,
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
    /// Updates the employee's legal name and work email.
    /// </summary>
    public void UpdateDetails(string legalName, string? workEmail)
    {
        LegalName = NormalizeRequired(legalName, nameof(legalName));
        WorkEmail = NormalizeOptional(workEmail);
    }

    /// <summary>
    /// Transfers the employee to a new department.
    /// Records an immutable history entry for retroactive reconstruction.
    /// </summary>
    public void TransferToDepartment(Guid departmentId, DateTimeOffset effectiveFrom, Guid changedBy, string? correlationId = null)
    {
        var previousValue = DepartmentId?.ToString() ?? "None";
        DepartmentId = departmentId;

        AddHistory(EmployeeHistoryType.DepartmentTransfer, previousValue, departmentId.ToString(), effectiveFrom, changedBy, correlationId);
    }

    /// <summary>
    /// Changes the employee's reporting manager.
    /// </summary>
    public void ChangeManager(Guid? managerId, DateTimeOffset effectiveFrom, Guid changedBy, string? correlationId = null)
    {
        var previousValue = ReportingManagerId?.ToString() ?? "None";
        ReportingManagerId = managerId;

        AddHistory(EmployeeHistoryType.ManagerChange, previousValue, managerId?.ToString() ?? "None", effectiveFrom, changedBy, correlationId);
    }

    /// <summary>
    /// Changes the employee's designation.
    /// </summary>
    public void ChangeDesignation(Guid designationId, DateTimeOffset effectiveFrom, Guid changedBy, string? correlationId = null)
    {
        var previousValue = DesignationId?.ToString() ?? "None";
        DesignationId = designationId;

        AddHistory(EmployeeHistoryType.DesignationChange, previousValue, designationId.ToString(), effectiveFrom, changedBy, correlationId);
    }

    /// <summary>
    /// Transfers the employee to a physical branch/location.
    /// </summary>
    public void TransferToBranch(Guid branchId, DateTimeOffset effectiveFrom, Guid changedBy, string? correlationId = null)
    {
        var previousValue = BranchId?.ToString() ?? "None";
        BranchId = branchId;

        AddHistory(EmployeeHistoryType.BranchTransfer, previousValue, branchId.ToString(), effectiveFrom, changedBy, correlationId);
    }

    /// <summary>
    /// Terminates the employee's employment.
    /// </summary>
    public void Terminate(DateTimeOffset effectiveFrom, Guid changedBy, string? correlationId = null)
    {
        if (Status == EmploymentStatus.Terminated) return;

        var previousStatus = Status.ToString();
        Status = EmploymentStatus.Terminated;

        AddHistory(EmployeeHistoryType.LifecycleStateChange, previousStatus, Status.ToString(), effectiveFrom, changedBy, correlationId);
    }

    private void AddHistory(
        EmployeeHistoryType type,
        string previousValue,
        string newValue,
        DateTimeOffset effectiveFrom,
        Guid changedBy,
        string? correlationId)
    {
        // Close the previous active entry of the same type if it exists
        var lastEntry = _history
            .Where(h => h.Type == type && h.EffectiveTo == null)
            .OrderByDescending(h => h.EffectiveFrom)
            .FirstOrDefault();

        lastEntry?.EffectiveTo = effectiveFrom;

        var entry = EmployeeHistoryEntry.Create(
            Id, type, previousValue, newValue, effectiveFrom, changedBy, correlationId);

        _history.Add(entry);
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
