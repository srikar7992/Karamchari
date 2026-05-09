using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Schedules.Constraints;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Orchestrates shift assignments using a composable constraints engine.
/// Prevents "if/else hell" by delegating validation to IShiftConstraint implementations.
/// </summary>
public sealed class ShiftPlanningService
{
    private readonly TimeAttendanceDbContext _db;
    private readonly IEnumerable<IShiftConstraint> _constraints;
    private readonly ILogger<ShiftPlanningService> _logger;

    public ShiftPlanningService(
        TimeAttendanceDbContext db, 
        IEnumerable<IShiftConstraint> constraints,
        ILogger<ShiftPlanningService> logger)
    {
        _db = db;
        _constraints = constraints;
        _logger = logger;
    }

    public async Task AssignShiftAsync(Guid scheduleId, Guid employeeId, Guid shiftId, DateOnly date, CancellationToken ct = default)
    {
        var schedule = await _db.WorkforceSchedules
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            ?? throw new InvalidOperationException("Schedule not found.");

        var allShifts = await _db.ShiftDefinitions.ToListAsync(ct);
        var policies = await _db.AttendancePolicies.ToListAsync(ct);

        var context = new SchedulingContext(schedule, allShifts, policies);

        var violations = _constraints
            .SelectMany(c => c.Evaluate(employeeId, shiftId, date, context))
            .ToList();

        if (violations.Any(v => v.Severity == ConstraintSeverity.Error))
        {
            var errors = string.Join("; ", violations.Where(v => v.Severity == ConstraintSeverity.Error).Select(v => v.Message));
            throw new InvalidOperationException($"Cannot assign shift: {errors}");
        }

        // Warnings are logged but don't block assignment
        foreach (var warning in violations.Where(v => v.Severity == ConstraintSeverity.Warning))
        {
            _logger.LogWarning("Scheduling warning for Employee {EmployeeId}: {Message}", employeeId, warning.Message);
        }

        schedule.AssignShift(employeeId, shiftId, date);
        await _db.SaveChangesAsync(ct);
    }
}
