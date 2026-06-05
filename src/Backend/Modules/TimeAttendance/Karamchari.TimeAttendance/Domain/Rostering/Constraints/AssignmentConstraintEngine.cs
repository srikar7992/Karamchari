namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

/// <summary>
/// Evaluates all hard assignment constraints in order.
/// Returns the first denial encountered — all constraints are blocking.
/// </summary>
public static class AssignmentConstraintEngine
{
    private static readonly IReadOnlyList<IAssignmentConstraint> HardConstraints =
    [
        new LeaveConstraint(),
        new SkillConstraint(),
        new OvertimeConstraint(),
        new RestPeriodConstraint(),
        new AvailabilityConstraint(),
    ];

    public static ConstraintResult Evaluate(RosterShift shift, AssignmentContext context)
    {
        foreach (var constraint in HardConstraints)
        {
            var result = constraint.Check(shift, context);
            if (!result.IsAllowed) return result;
        }

        return ConstraintResult.Allow();
    }

    /// <summary>
    /// Evaluates all constraints and returns every violation (for publish-time reporting).
    /// </summary>
    public static IReadOnlyList<ConstraintResult> EvaluateAll(RosterShift shift, AssignmentContext context)
    {
        return HardConstraints
            .Select(c => c.Check(shift, context))
            .Where(r => !r.IsAllowed)
            .ToList();
    }
}
