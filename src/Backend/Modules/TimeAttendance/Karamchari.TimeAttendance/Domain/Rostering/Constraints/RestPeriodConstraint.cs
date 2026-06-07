namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed class RestPeriodConstraint(double minimumRestHours = 11.0) : IAssignmentConstraint
{
    private readonly double _minimumRestHours = minimumRestHours;

    public string Name => "RestPeriod";

    public ConstraintResult Check(RosterShift shift, AssignmentContext context)
    {
        var shiftStart = shift.WorkDate.ToDateTime(shift.StartTime);
        DateTime shiftEnd = shift.IsOvernight
            ? shift.WorkDate.AddDays(1).ToDateTime(shift.EndTime)
            : shift.WorkDate.ToDateTime(shift.EndTime);

        foreach (AssignedShiftWindow prev in context.WeeklyAssignments)
        {
            var prevStart = prev.WorkDate.ToDateTime(prev.StartTime);
            DateTime prevEnd = prev.EndTime < prev.StartTime // overnight
                ? prev.WorkDate.AddDays(1).ToDateTime(prev.EndTime)
                : prev.WorkDate.ToDateTime(prev.EndTime);

            // Rest gap between previous shift end and new shift start
            double restAfterPrev = (shiftStart - prevEnd).TotalHours;
            if (restAfterPrev >= 0 && restAfterPrev < _minimumRestHours)
                return ConstraintResult.Deny("REST_VIOLATION",
                    $"Only {restAfterPrev:F1}h rest after shift on {prev.WorkDate:d}. Minimum {_minimumRestHours}h required.");

            // Rest gap between new shift end and existing shift start
            double restBeforeNext = (prevStart - shiftEnd).TotalHours;
            if (restBeforeNext >= 0 && restBeforeNext < _minimumRestHours)
                return ConstraintResult.Deny("REST_VIOLATION",
                    $"Only {restBeforeNext:F1}h rest before shift on {prev.WorkDate:d}. Minimum {_minimumRestHours}h required.");
        }

        return ConstraintResult.Allow();
    }
}
