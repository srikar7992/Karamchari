using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Rostering.Constraints;
using Karamchari.TimeAttendance.Domain.Rostering.Events;

namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed class RosterShiftAssignment : Entity<Guid>
{
    private RosterShiftAssignment() { }

    public Guid RosterId { get; private set; }
    public Guid RosterShiftId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly WorkDate { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    internal static RosterShiftAssignment Create(
        Guid rosterId, Guid rosterShiftId, Guid employeeId, DateOnly workDate)
    {
        return new RosterShiftAssignment
        {
            Id = Guid.NewGuid(),
            RosterId = rosterId,
            RosterShiftId = rosterShiftId,
            EmployeeId = employeeId,
            WorkDate = workDate,
            Status = AssignmentStatus.Assigned,
            AssignedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    internal void Cancel() => Status = AssignmentStatus.Cancelled;
    internal void Complete() => Status = AssignmentStatus.Completed;
    internal void Accept() => Status = AssignmentStatus.Accepted;
    internal void Reject() => Status = AssignmentStatus.Rejected;
}

/// <summary>
/// Central roster aggregate. Owns both shifts (what work exists) and assignments (who does it).
/// Assign() enforces hard constraints before creating an assignment — invalid assignments are rejected, not warned.
/// Publish() raises ScheduledShiftCreated per active assignment for Phase 3 Attendance consumption.
/// </summary>
public sealed class Roster : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<RosterShift> _shifts = [];
    private readonly List<RosterShiftAssignment> _assignments = [];

    private Roster() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public RosterStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// Optimistic concurrency token. SQL Server manages this value — never set in application code.
    /// EF Core uses it to detect concurrent modifications: two schedulers racing on the same roster
    /// will produce a DbUpdateConcurrencyException on the second SaveChanges, which the API layer
    /// returns as 409 Conflict.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    public IReadOnlyCollection<RosterShift> Shifts => _shifts.AsReadOnly();
    public IReadOnlyCollection<RosterShiftAssignment> Assignments => _assignments.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Roster Create(string tenantId, string name, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be after start date.");

        var roster = new Roster
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = RosterStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        roster.RaiseDomainEvent(new RosterCreated(roster.Id, tenantId, startDate, endDate));
        return roster;
    }

    // ── Shift management ──────────────────────────────────────────────────────

    public RosterShift AddShift(
        string name,
        ShiftType type,
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid locationId,
        int requiredHeadcount,
        IEnumerable<SkillRequirement>? requiredSkills = null)
    {
        EnsureModifiable();

        if (workDate < StartDate || workDate > EndDate)
            throw new InvalidOperationException(
                $"Work date {workDate} is outside roster period {StartDate}–{EndDate}.");

        var shift = RosterShift.Create(
            Id, TenantId, name, type, workDate, startTime, endTime,
            locationId, requiredHeadcount, requiredSkills);

        _shifts.Add(shift);
        return shift;
    }

    // ── Assignment management ─────────────────────────────────────────────────

    /// <summary>
    /// Creates an assignment after running all hard constraints.
    /// Throws <see cref="InvalidOperationException"/> if any constraint is violated — callers must not swallow.
    /// </summary>
    public RosterShiftAssignment Assign(
        Guid employeeId,
        Guid rosterShiftId,
        AssignmentContext context)
    {
        EnsureModifiable();

        var shift = _shifts.FirstOrDefault(s => s.Id == rosterShiftId)
            ?? throw new InvalidOperationException($"Shift {rosterShiftId} not found in roster.");

        if (_assignments.Any(a =>
            a.EmployeeId == employeeId &&
            a.RosterShiftId == rosterShiftId &&
            a.Status != AssignmentStatus.Cancelled))
            throw new InvalidOperationException(
                $"Employee {employeeId} is already assigned to shift {rosterShiftId}.");

        var constraint = AssignmentConstraintEngine.Evaluate(shift, context);
        if (!constraint.IsAllowed)
            throw new InvalidOperationException(
                $"[{constraint.ViolationCode}] {constraint.Reason}");

        var assignment = RosterShiftAssignment.Create(Id, rosterShiftId, employeeId, shift.WorkDate);
        _assignments.Add(assignment);

        RaiseDomainEvent(new ShiftAssigned(Id, TenantId, assignment.Id, employeeId, shift.WorkDate));
        return assignment;
    }

    public void Unassign(Guid assignmentId)
    {
        EnsureModifiable();

        var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException($"Assignment {assignmentId} not found.");

        assignment.Cancel();
        RaiseDomainEvent(new ShiftUnassigned(Id, TenantId, assignmentId, assignment.EmployeeId));
    }

    // ── Status transitions ────────────────────────────────────────────────────

    public void MoveToReview()
    {
        if (Status != RosterStatus.Draft)
            throw new InvalidOperationException("Only Draft rosters can be moved to Review.");

        Status = RosterStatus.Review;
        RaiseDomainEvent(new RosterMovedToReview(Id, TenantId));
    }

    /// <summary>
    /// Publishes the roster. Caller must run <see cref="Validation.RosterPublishValidator"/> first
    /// and return 422 if violations exist — this method assumes validation passed.
    /// Raises <see cref="ScheduledShiftCreated"/> per active assignment for Attendance (Phase 3) consumption.
    /// </summary>
    public void Publish()
    {
        if (Status is not (RosterStatus.Draft or RosterStatus.Review))
            throw new InvalidOperationException("Only Draft or Review rosters can be published.");

        Status = RosterStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;

        var shiftMap = _shifts.ToDictionary(s => s.Id);
        var activeAssignments = _assignments.Where(a => a.Status != AssignmentStatus.Cancelled).ToList();

        foreach (var assignment in activeAssignments)
        {
            if (!shiftMap.TryGetValue(assignment.RosterShiftId, out var shift)) continue;

            RaiseDomainEvent(new ScheduledShiftCreated(
                TenantId, Id, shift.Id, assignment.Id,
                assignment.EmployeeId, shift.WorkDate,
                shift.StartTime, shift.EndTime));
        }

        RaiseDomainEvent(new RosterPublished(Id, TenantId, StartDate, EndDate, activeAssignments.Count, PublishedAt.Value));
    }

    public void Archive()
    {
        if (Status == RosterStatus.Archived)
            throw new InvalidOperationException("Roster is already archived.");

        Status = RosterStatus.Archived;
    }

    private void EnsureModifiable()
    {
        if (Status == RosterStatus.Published)
            throw new InvalidOperationException("Published roster cannot be modified directly. Create a revision instead.");

        if (Status == RosterStatus.Archived)
            throw new InvalidOperationException("Archived roster cannot be modified.");
    }
}
