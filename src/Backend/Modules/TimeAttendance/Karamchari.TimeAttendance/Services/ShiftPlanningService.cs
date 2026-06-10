// -----------------------------------------------------------------------
// <copyright file="ShiftPlanningService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.TimeAttendance.Domain.Compliance;
using Karamchari.TimeAttendance.Domain.Schedules;
using Karamchari.TimeAttendance.Domain.Schedules.Constraints;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Orchestrates shift assignments using a composable constraints engine.
/// Prevents "if/else hell" by delegating validation to IShiftConstraint implementations.
/// </summary>
public sealed class ShiftPlanningService(
    TimeAttendanceDbContext db,
    IEnumerable<IShiftConstraint> constraints,
    ILogger<ShiftPlanningService> logger)
{
    private readonly TimeAttendanceDbContext _db = db;
    private readonly IEnumerable<IShiftConstraint> _constraints = constraints;
    private readonly ILogger<ShiftPlanningService> _logger = logger;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task AssignShiftAsync(Guid scheduleId, Guid employeeId, Guid shiftId, DateOnly workDate, CancellationToken ct = default)
    {
        WorkforceSchedule schedule = await _db.WorkforceSchedules
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            ?? throw new InvalidOperationException("Schedule not found.");

        List<ShiftDefinition> allShifts = await _db.ShiftDefinitions.ToListAsync(ct);
        List<AttendancePolicy> policies = await _db.AttendancePolicies.ToListAsync(ct);

        var context = new SchedulingContext(schedule, allShifts, policies);

        var violations = _constraints
            .SelectMany(c => c.Evaluate(employeeId, shiftId, workDate, context))
            .ToList();

        if (violations.Any(v => v.Severity == ConstraintSeverity.Error))
        {
            string errors = string.Join("; ", violations.Where(v => v.Severity == ConstraintSeverity.Error).Select(v => v.Message));
            throw new InvalidOperationException($"Cannot assign shift: {errors}");
        }

        // Warnings are logged but don't block assignment
        foreach (ConstraintViolation? warning in violations.Where(v => v.Severity == ConstraintSeverity.Warning))
        {
            _logger.LogWarning("Scheduling warning for Employee {EmployeeId}: {Message}", employeeId, warning.Message);
        }

        schedule.AssignShift(employeeId, shiftId, workDate);
        await _db.SaveChangesAsync(ct);
    }
}
