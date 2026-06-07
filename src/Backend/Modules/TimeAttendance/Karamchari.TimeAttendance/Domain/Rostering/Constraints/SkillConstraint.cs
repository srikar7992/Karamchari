namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed class SkillConstraint : IAssignmentConstraint
{
    public string Name => "Skill";

    public ConstraintResult Check(RosterShift shift, AssignmentContext context)
    {
        foreach (SkillRequirement req in shift.RequiredSkills)
        {
            EmployeeSkill? match = context.Skills.FirstOrDefault(
                s => s.SkillId == req.SkillId && s.IsValid(shift.WorkDate));

            if (match is null)
                return ConstraintResult.Deny("SKILL_MISSING",
                    $"Employee lacks required skill '{req.SkillName}'.");

            if (match.Level < req.MinimumLevel)
                return ConstraintResult.Deny("SKILL_LEVEL_INSUFFICIENT",
                    $"Skill '{req.SkillName}' is level {match.Level}, requires level {req.MinimumLevel}.");
        }

        return ConstraintResult.Allow();
    }
}
