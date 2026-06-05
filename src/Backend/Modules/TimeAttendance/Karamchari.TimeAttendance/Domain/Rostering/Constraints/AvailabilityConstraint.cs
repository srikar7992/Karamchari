namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed class AvailabilityConstraint : IAssignmentConstraint
{
    public string Name => "Availability";

    public ConstraintResult Check(RosterShift shift, AssignmentContext context)
    {
        if (context.AvailabilityWindows.Count == 0)
            return ConstraintResult.Allow(); // no declared preference — no restriction

        var dayWindows = context.AvailabilityWindows
            .Where(w => w.DayOfWeek == shift.WorkDate.DayOfWeek)
            .ToList();

        if (dayWindows.Count == 0)
            return ConstraintResult.Deny("AVAILABILITY_CONFLICT",
                $"Employee has no availability declared on {shift.WorkDate.DayOfWeek}.");

        // For overnight shifts only start-time must fall within a window (end-time check skipped).
        var fits = shift.IsOvernight
            ? dayWindows.Any(w => shift.StartTime >= w.From)
            : dayWindows.Any(w => shift.StartTime >= w.From && shift.EndTime <= w.To);

        return fits
            ? ConstraintResult.Allow()
            : ConstraintResult.Deny("AVAILABILITY_CONFLICT",
                $"Shift {shift.StartTime:HH\\:mm}–{shift.EndTime:HH\\:mm} falls outside declared availability on {shift.WorkDate.DayOfWeek}.");
    }
}
