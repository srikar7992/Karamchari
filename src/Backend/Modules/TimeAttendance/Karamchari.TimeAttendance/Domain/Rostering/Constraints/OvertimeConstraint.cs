namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed class OvertimeConstraint : IAssignmentConstraint
{
    public string Name => "Overtime";

    public ConstraintResult Check(RosterShift shift, AssignmentContext context)
    {
        decimal max = context.OvertimePolicy.MaximumHours;
        if (max <= 0) return ConstraintResult.Allow();

        decimal shiftHours = (decimal)shift.Duration.TotalHours;
        decimal projected = context.WeeklyHoursWorked + shiftHours;

        return projected > max
            ? ConstraintResult.Deny("OVERTIME_VIOLATION",
                $"Assignment would result in {projected:F1}h this {context.OvertimePolicy.MeasurementWindow}. Maximum is {max}h.")
            : ConstraintResult.Allow();
    }
}
