namespace Karamchari.TimeAttendance.Domain.Timesheets;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Aggregate root representing a weekly collection of time entries for an employee.
/// </summary>
public sealed class Timesheet : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<TimeEntry> _entries = new();

    private Timesheet(Guid id, Guid employeeId, DateOnly weekStartDate)
        : base(id)
    {
        EmployeeId = employeeId;
        WeekStartDate = weekStartDate;
        Status = TimesheetStatus.Draft;
    }

    private Timesheet()
    {
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the start date of the week (usually a Monday).
    /// </summary>
    public DateOnly WeekStartDate { get; private set; }

    /// <summary>
    /// Gets the current status of the timesheet.
    /// </summary>
    public TimesheetStatus Status { get; private set; }

    /// <summary>
    /// Gets the list of time entries for the week.
    /// </summary>
    public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Gets the total hours billable in this timesheet.
    /// </summary>
    public decimal TotalHours => _entries.Sum(x => x.Hours);

    /// <summary>
    /// Creates a new timesheet for an employee.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="weekStartDate">The start of the work week.</param>
    /// <returns>A new <see cref="Timesheet"/> instance.</returns>
    public static Timesheet Create(Guid employeeId, DateOnly weekStartDate)
    {
        return new Timesheet(Guid.NewGuid(), employeeId, weekStartDate);
    }

    /// <summary>
    /// Updates the entries for the timesheet. Only allowed in Draft or Rejected status.
    /// </summary>
    /// <param name="entries">The new set of entries.</param>
    public void UpdateEntries(IEnumerable<TimeEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (Status != TimesheetStatus.Draft && Status != TimesheetStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot update entries when timesheet is in {Status} status.");
        }

        _entries.Clear();
        foreach (var entry in entries)
        {
            entry.Validate();
            _entries.Add(entry);
        }
    }

    /// <summary>
    /// Submits the timesheet for manager approval.
    /// </summary>
    public void Submit()
    {
        if (Status != TimesheetStatus.Draft && Status != TimesheetStatus.Rejected)
        {
            throw new InvalidOperationException("Only Draft or Rejected timesheets can be submitted.");
        }

        if (TotalHours <= 0)
        {
            throw new InvalidOperationException("Cannot submit a timesheet with zero total hours.");
        }

        Status = TimesheetStatus.Submitted;
    }

    /// <summary>
    /// Approves the timesheet.
    /// </summary>
    public void Approve()
    {
        if (Status != TimesheetStatus.Submitted)
        {
            throw new InvalidOperationException("Only Submitted timesheets can be approved.");
        }

        Status = TimesheetStatus.Approved;
        
        // This will emit an integration event to Payroll later.
    }

    /// <summary>
    /// Rejects the timesheet.
    /// </summary>
    /// <param name="reason">The reason for rejection.</param>
    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != TimesheetStatus.Submitted)
        {
            throw new InvalidOperationException("Only Submitted timesheets can be rejected.");
        }

        Status = TimesheetStatus.Rejected;
    }
}
