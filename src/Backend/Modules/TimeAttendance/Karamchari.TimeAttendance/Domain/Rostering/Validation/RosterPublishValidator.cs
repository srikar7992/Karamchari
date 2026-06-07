using Karamchari.TimeAttendance.Domain.Rostering.Constraints;

namespace Karamchari.TimeAttendance.Domain.Rostering.Validation;

/// <summary>
/// Validates a roster before it can be published.
/// Called by the API layer — if violations exist the endpoint returns 422, Roster.Publish() is never called.
/// Checks: headcount, skill mix, coverage rules, and per-assignment hard constraints.
/// </summary>
public static class RosterPublishValidator
{
    public static PublishValidationResult Validate(
        Roster roster,
        IReadOnlyList<RosterShift> shifts,
        IReadOnlyList<CoverageRule> coverageRules,
        IReadOnlyDictionary<Guid, AssignmentContext> employeeContexts)
    {
        var result = new PublishValidationResult();
        var activeAssignmentsByShift = roster.Assignments
            .Where(a => a.Status != AssignmentStatus.Cancelled)
            .GroupBy(a => a.RosterShiftId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 1. Per-shift: headcount + skill mix + constraint re-check
        foreach (RosterShift shift in shifts)
        {
            List<RosterShiftAssignment> assigned = activeAssignmentsByShift.TryGetValue(shift.Id, out List<RosterShiftAssignment>? list) ? list : [];

            // Headcount
            if (assigned.Count < shift.RequiredHeadcount)
                result.Add(new PublishViolation(
                    "HEADCOUNT_INSUFFICIENT",
                    $"Shift '{shift.Name}' on {shift.WorkDate:d} needs {shift.RequiredHeadcount} staff, {assigned.Count} assigned.",
                    shift.WorkDate, shift.Id));

            // Skill mix
            foreach (SkillRequirement req in shift.RequiredSkills)
            {
                int qualified = assigned.Count(a =>
                    employeeContexts.TryGetValue(a.EmployeeId, out AssignmentContext? ctx) &&
                    ctx.Skills.Any(s =>
                        s.SkillId == req.SkillId &&
                        s.IsValid(shift.WorkDate) &&
                        s.Level >= req.MinimumLevel));

                if (qualified < req.Count)
                    result.Add(new PublishViolation(
                        "SKILL_MIX_INSUFFICIENT",
                        $"Shift '{shift.Name}' on {shift.WorkDate:d} requires {req.Count}x '{req.SkillName}' (level {req.MinimumLevel}+), only {qualified} qualify.",
                        shift.WorkDate, shift.Id));
            }

            // Re-check hard constraints per assignee (conditions may have changed since assignment)
            foreach (RosterShiftAssignment? assignment in assigned)
            {
                if (!employeeContexts.TryGetValue(assignment.EmployeeId, out AssignmentContext? ctx)) continue;

                IReadOnlyList<ConstraintResult> violations = AssignmentConstraintEngine.EvaluateAll(shift, ctx);
                foreach (ConstraintResult v in violations)
                    result.Add(new PublishViolation(
                        v.ViolationCode!,
                        $"Employee {assignment.EmployeeId} on '{shift.Name}' ({shift.WorkDate:d}): {v.Reason}",
                        shift.WorkDate, shift.Id));
            }
        }

        // 2. Coverage rules — per date, per location
        var shiftsByLocation = shifts.GroupBy(s => s.LocationId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (CoverageRule? rule in coverageRules.Where(r => r.IsActive))
        {
            if (!shiftsByLocation.TryGetValue(rule.LocationId, out List<RosterShift>? locationShifts)) continue;

            IEnumerable<DateOnly> datesInRoster = Enumerable
                .Range(0, roster.EndDate.DayNumber - roster.StartDate.DayNumber + 1)
                .Select(d => roster.StartDate.AddDays(d));

            foreach (DateOnly date in datesInRoster)
            {
                var shiftsOnDate = locationShifts.Where(s => s.WorkDate == date).ToList();
                if (shiftsOnDate.Count == 0) continue;

                int totalAssigned = shiftsOnDate
                    .SelectMany(s => activeAssignmentsByShift.TryGetValue(s.Id, out List<RosterShiftAssignment>? l) ? l : [])
                    .Count();

                if (totalAssigned < rule.MinimumStaff)
                    result.Add(new PublishViolation(
                        "COVERAGE_INSUFFICIENT",
                        $"Location {rule.LocationId} on {date:d} requires min {rule.MinimumStaff} staff, {totalAssigned} assigned.",
                        date));
            }
        }

        return result;
    }
}
