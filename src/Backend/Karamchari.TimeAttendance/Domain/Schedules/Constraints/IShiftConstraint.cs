using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Domain.Compliance;

namespace Karamchari.TimeAttendance.Domain.Schedules.Constraints;

public enum ConstraintSeverity { Information, Warning, Error }

public sealed record ConstraintViolation(string RuleName, string Message, ConstraintSeverity Severity);

public sealed record SchedulingContext(
    WorkforceSchedule Schedule,
    IEnumerable<ShiftDefinition> AllShifts,
    IEnumerable<AttendancePolicy> Policies);

/// <summary>
/// Interface for composable scheduling rules.
/// Prevents "if/else hell" in the scheduling service.
/// </summary>
public interface IShiftConstraint
{
    string Name { get; }
    IEnumerable<ConstraintViolation> Evaluate(Guid employeeId, Guid shiftId, DateOnly date, SchedulingContext context);
}
