namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public interface IAssignmentConstraint
{
    string Name { get; }
    ConstraintResult Check(RosterShift shift, AssignmentContext context);
}
