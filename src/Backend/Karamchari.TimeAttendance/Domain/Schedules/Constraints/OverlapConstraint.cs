namespace Karamchari.TimeAttendance.Domain.Schedules.Constraints;

/// <summary>
/// Basic constraint to prevent double-booking an employee on the same day.
/// </summary>
public sealed class OverlapConstraint : IShiftConstraint
{
    public string Name => "OverlapPrevention";

    public IEnumerable<ConstraintViolation> Evaluate(Guid employeeId, Guid shiftId, DateOnly date, SchedulingContext context)
    {
        if (context.Schedule.Assignments.Any(a => a.EmployeeId == employeeId && a.Date == date))
        {
            yield return new ConstraintViolation(
                Name, 
                $"Employee already has a shift assignment on {date}.", 
                ConstraintSeverity.Error);
        }
    }
}
