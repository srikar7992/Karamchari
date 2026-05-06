using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Timesheets.Events;

namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Aggregate root for a weekly collection of time entries for one employee.
///
/// Lifecycle:  Draft → Submitted → Approved
///                ↑                    │ (Reopen for retroactive edit)
///                └────────────────────┘
///
/// Row-level approval: individual entries move to Approved via <see cref="ApproveEntry"/>.
/// When every entry is approved the timesheet auto-approves and publishes
/// <see cref="TimesheetApproved"/> through the MassTransit outbox.
/// </summary>
public sealed class Timesheet : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<TimeEntry> _entries = [];
    private readonly List<TimesheetAuditEntry> _auditLog = [];

    private Timesheet(Guid id, Guid employeeId, DateOnly weekStartDate, string employeeTimeZoneId)
        : base(id)
    {
        EmployeeId = employeeId;
        WeekStartDate = weekStartDate;
        EmployeeTimeZoneId = employeeTimeZoneId;
        Status = TimesheetStatus.Draft;
    }

    // EF Core materialisation constructor
    private Timesheet() { TenantId = string.Empty; }

    // ── Identity & ownership ────────────────────────────────────────────────

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    // ── Period ──────────────────────────────────────────────────────────────

    /// <summary>Monday of the work week (ISO 8601).</summary>
    public DateOnly WeekStartDate { get; private set; }

    /// <summary>
    /// IANA timezone id for the employee (e.g. "Asia/Kolkata", "America/New_York").
    /// Stored so the UI can convert UTC timestamps to the employee's local display time
    /// without losing the authoritative UTC values.
    /// </summary>
    public string EmployeeTimeZoneId { get; private set; } = "UTC";

    // ── State ───────────────────────────────────────────────────────────────

    public TimesheetStatus Status { get; private set; }

    /// <summary>UTC timestamp of the most recent approval (null while Draft/Submitted/Rejected).</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>
    /// True after <see cref="Reopen"/> has been called at least once.
    /// Propagated to <see cref="TimesheetApproved"/> so downstream systems know to
    /// reverse previously posted amounts before applying the new figures.
    /// </summary>
    public bool IsRetroactive { get; private set; }

    /// <summary>
    /// SQL Server rowversion — EF Core concurrency token.
    /// Prevents silent overwrites when two users edit the same timesheet concurrently.
    /// EF Core throws DbUpdateConcurrencyException on conflict; the API maps this to HTTP 409.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; private set; }

    // ── Collections ─────────────────────────────────────────────────────────

    public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();
    public IReadOnlyCollection<TimesheetAuditEntry> AuditLog => _auditLog.AsReadOnly();

    /// <summary>Sum of all entry hours. Computed in-memory from the JSON entries column.</summary>
    public decimal TotalHours => _entries.Sum(x => x.Hours);

    // ── Factory ─────────────────────────────────────────────────────────────

    /// <param name="employeeId">Employee owner of this timesheet.</param>
    /// <param name="weekStartDate">ISO week start (Monday).</param>
    /// <param name="employeeTimeZoneId">IANA tz id (default "UTC").</param>
    public static Timesheet Create(Guid employeeId, DateOnly weekStartDate, string employeeTimeZoneId = "UTC")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeTimeZoneId);
        return new Timesheet(Guid.NewGuid(), employeeId, weekStartDate, employeeTimeZoneId);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the full entry set. Only permitted while <see cref="Status"/> is Draft or Rejected.
    /// Cross-midnight splitting and capacity checks must be applied by the caller
    /// (<see cref="Services.TimesheetService"/>) before invoking this method.
    /// </summary>
    public void UpdateEntries(IEnumerable<TimeEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
            throw new InvalidOperationException(
                $"Entries cannot be updated when timesheet is {Status}. Call Reopen first.");

        var list = entries.ToList();

        foreach (var entry in list)
            entry.Validate();

        TimesheetValidator.ValidateEntries(list);

        var existingVersions = _entries.ToDictionary(e => e.EntryId, e => e.Version);

        _entries.Clear();
        foreach (var entry in list)
        {
            // Increment version if this EntryId already existed (i.e., it was edited)
            var newVersion = existingVersions.TryGetValue(entry.EntryId, out var prev) ? prev + 1 : 1;
            _entries.Add(entry with { Version = newVersion, ApprovedAtVersion = null });
        }
    }

    /// <summary>
    /// Submits the timesheet for manager approval.
    /// </summary>
    /// <param name="actorId">The employee or delegate submitting.</param>
    public void Submit(Guid actorId)
    {
        if (Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
            throw new InvalidOperationException("Only Draft or Rejected timesheets can be submitted.");

        if (TotalHours <= 0)
            throw new InvalidOperationException("Cannot submit a timesheet with zero total hours.");

        Status = TimesheetStatus.Submitted;
        _entries.ReplaceAll(e => e with { Status = TimeEntryStatus.Submitted });
        Audit("Submitted", actorId);
    }

    /// <summary>
    /// Approves a single entry by its stable <see cref="TimeEntry.EntryId"/>.
    /// When every entry is approved the timesheet auto-approves.
    /// </summary>
    /// <param name="entryId">Stable entry identifier.</param>
    /// <param name="approverId">Manager or delegate performing the approval.</param>
    public void ApproveEntry(Guid entryId, Guid approverId)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Row-level approval requires the timesheet to be Submitted.");

        var idx = _entries.FindIndex(e => e.EntryId == entryId);
        if (idx < 0)
            throw new ArgumentException($"Entry {entryId} not found in timesheet {Id}.", nameof(entryId));

        var current = _entries[idx];

        // Guard: if entry was edited after approval (version mismatch), require re-submission
        if (current.ApprovedAtVersion.HasValue && current.ApprovedAtVersion != current.Version)
            throw new InvalidOperationException(
                $"Entry {entryId} was modified after approval. The timesheet must be resubmitted.");

        _entries[idx] = current with
        {
            Status = TimeEntryStatus.Approved,
            ApprovedAtVersion = current.Version,
        };
        Audit("EntryApproved", approverId);

        if (_entries.All(e => e.Status == TimeEntryStatus.Approved))
            Approve(approverId);
    }

    /// <summary>
    /// Approves the entire timesheet (all entries) in one operation.
    /// Raises <see cref="TimesheetApproved"/> through the domain-event pipeline.
    /// </summary>
    /// <param name="approverId">Manager or delegate performing the approval.</param>
    public void Approve(Guid approverId)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only Submitted timesheets can be approved.");

        Status = TimesheetStatus.Approved;
        ApprovedAt = DateTimeOffset.UtcNow;
        // Stamp ApprovedAtVersion on all entries for future retroactive change detection
        _entries.ReplaceAll(e => e with
        {
            Status = TimeEntryStatus.Approved,
            ApprovedAtVersion = e.Version,
        });
        Audit("Approved", approverId);

        RaiseDomainEvent(new TimesheetApproved(
            Id,
            EmployeeId,
            TenantId,
            WeekStartDate,
            EmployeeTimeZoneId,
            _entries.AsReadOnly(),
            IsRetroactive));
    }

    /// <summary>
    /// Rejects the timesheet and returns it to the employee.
    /// </summary>
    /// <param name="reason">Mandatory rejection reason shown to the employee.</param>
    /// <param name="actorId">Manager or delegate performing the rejection.</param>
    public void Reject(string reason, Guid actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only Submitted timesheets can be rejected.");

        Status = TimesheetStatus.Rejected;
        _entries.ReplaceAll(e => e with { Status = TimeEntryStatus.Rejected });
        Audit("Rejected", actorId, reason);
    }

    /// <summary>
    /// Reopens an approved timesheet for retroactive correction.
    /// Sets <see cref="IsRetroactive"/> = true permanently so subsequent approvals
    /// are flagged as corrections to downstream systems.
    /// </summary>
    /// <param name="reason">Mandatory justification recorded in the audit log.</param>
    /// <param name="adminId">Administrator authorising the reopen.</param>
    public void Reopen(string reason, Guid adminId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != TimesheetStatus.Approved)
            throw new InvalidOperationException("Only Approved timesheets can be reopened for retroactive editing.");

        Status = TimesheetStatus.Draft;
        ApprovedAt = null;
        IsRetroactive = true;
        _entries.ReplaceAll(e => e with { Status = TimeEntryStatus.Draft });
        Audit("Reopened", adminId, reason);

        RaiseDomainEvent(new TimesheetReopened(
            Id, EmployeeId, TenantId, WeekStartDate, adminId, reason));
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private void Audit(string action, Guid actorId, string? reason = null) =>
        _auditLog.Add(new TimesheetAuditEntry
        {
            Action = action,
            ActorId = actorId,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow,
        });
}

// Local extension to keep the aggregate methods readable.
file static class ListExtensions
{
    internal static void ReplaceAll<T>(this List<T> list, Func<T, T> transform)
    {
        for (var i = 0; i < list.Count; i++)
            list[i] = transform(list[i]);
    }
}
