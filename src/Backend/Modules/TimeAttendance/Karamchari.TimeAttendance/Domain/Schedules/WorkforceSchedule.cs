// -----------------------------------------------------------------------
// <copyright file="WorkforceSchedule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Schedules;

/// <summary>
/// Aggregate root representing a departmental or organizational roster for a period.
/// Orchestrates shift assignments and enforces scheduling constraints.
/// </summary>
public sealed class WorkforceSchedule : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<ShiftAssignment> _assignments = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";
    public Guid? LocationId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly StartDate { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<ShiftAssignment> Assignments => _assignments.AsReadOnly();

    private WorkforceSchedule() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static WorkforceSchedule Create(string tenantId, string name, string tzId, DateOnly start, DateOnly end, Guid? locationId = null)
    {
        if (end < start) throw new ArgumentException("End date must be after start.");

        return new WorkforceSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            TimeZoneId = tzId,
            StartDate = start,
            EndDate = end,
            LocationId = locationId
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AssignShift(Guid employeeId, Guid shiftId, DateOnly date)
    {
        if (date < StartDate || date > EndDate)
            throw new InvalidOperationException("Assignment date is outside schedule range.");

        // Check for existing assignment to prevent overlap
        if (_assignments.Any(a => a.EmployeeId == employeeId && a.Date == date))
            throw new InvalidOperationException($"Employee {employeeId} already assigned on {date}.");

        _assignments.Add(ShiftAssignment.Create(Id, employeeId, shiftId, date));
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class ShiftAssignment : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ScheduleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ShiftId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly Date { get; private set; }

    private ShiftAssignment() { }

    internal static ShiftAssignment Create(Guid scheduleId, Guid employeeId, Guid shiftId, DateOnly date)
    {
        return new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ScheduleId = scheduleId,
            EmployeeId = employeeId,
            ShiftId = shiftId,
            Date = date
        };
    }
}
