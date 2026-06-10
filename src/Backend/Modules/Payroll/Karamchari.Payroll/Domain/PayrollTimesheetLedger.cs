// -----------------------------------------------------------------------
// <copyright file="PayrollTimesheetLedger.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// A localized read model in the Payroll context that stores approved billable hours.
/// This ensures the Payroll module remains isolated from the Time &amp; Attendance database.
/// </summary>
public sealed class PayrollTimesheetLedger : ITenantOwned
{
    private PayrollTimesheetLedger(
        Guid id,
        Guid timesheetId,
        Guid employeeId,
        DateOnly weekStartDate,
        decimal totalHours)
    {
        Id = id;
        TimesheetId = timesheetId;
        EmployeeId = employeeId;
        WeekStartDate = weekStartDate;
        TotalHours = totalHours;
        IsProcessed = false;
    }

    private PayrollTimesheetLedger()
    {
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier for the ledger entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the original timesheet identifier.
    /// </summary>
    public Guid TimesheetId { get; private set; }

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the start date of the week.
    /// </summary>
    public DateOnly WeekStartDate { get; private set; }

    /// <summary>
    /// Gets the total approved hours.
    /// </summary>
    public decimal TotalHours { get; private set; }

    /// <summary>
    /// Gets a value indicating whether these hours have already been included in a payroll run.
    /// </summary>
    public bool IsProcessed { get; private set; }

    /// <summary>
    /// Records an approved timesheet in the ledger.
    /// </summary>
    /// <param name="timesheetId">The timesheet identifier.</param>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="weekStartDate">The week start date.</param>
    /// <param name="totalHours">The total hours.</param>
    /// <returns>A new <see cref="PayrollTimesheetLedger"/> instance.</returns>
    public static PayrollTimesheetLedger Record(Guid timesheetId, Guid employeeId, DateOnly weekStartDate, decimal totalHours)
    {
        return new PayrollTimesheetLedger(Guid.NewGuid(), timesheetId, employeeId, weekStartDate, totalHours);
    }

    /// <summary>
    /// Marks the ledger entry as processed by a payroll run.
    /// </summary>
    public void MarkAsProcessed() => IsProcessed = true;
}
