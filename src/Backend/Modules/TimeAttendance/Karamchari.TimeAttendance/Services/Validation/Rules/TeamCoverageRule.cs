namespace Karamchari.TimeAttendance.Services.Validation.Rules;

using Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Ensures team coverage threshold is not breached.
/// Example: max 30% of team on leave simultaneously.
/// MinCoveragePercent is stored on LeaveBlackoutPeriod or fetched from team policy (future).
/// For now uses a simple count: if team calendar already has &gt;= threshold entries on any day, block.
/// </summary>
public sealed class TeamCoverageRule : ILeaveValidationRule
{
    private readonly int _teamSize;
    private readonly int _maxAbsencePercent;

    public TeamCoverageRule(int teamSize, int maxAbsencePercent = 30)
    {
        _teamSize = teamSize;
        _maxAbsencePercent = maxAbsencePercent;
    }

    public string RuleName => "TeamCoverage";

    public Task<LeaveValidationResult> ValidateAsync(LeaveValidationContext context, CancellationToken ct = default)
    {
        if (_teamSize <= 0) return Task.FromResult(LeaveValidationResult.Pass());

        var maxAllowed = (int)Math.Floor(_teamSize * _maxAbsencePercent / 100.0);
        var start = context.Request.StartDate;
        var end = context.Request.EndDate;

        var current = start;
        while (current <= end)
        {
            var onLeaveCount = context.TeamCalendar.Count(e =>
                e.Date == current &&
                e.EmployeeId != context.Request.EmployeeId &&
                e.Status is not (LeaveCalendarEntryStatus.Cancelled
                    or LeaveCalendarEntryStatus.Rejected));

            if (onLeaveCount >= maxAllowed)
            {
                return Task.FromResult(LeaveValidationResult.Fail(
                    RuleName,
                    $"Team coverage threshold breached on {current:d}. Max {_maxAbsencePercent}% ({maxAllowed} of {_teamSize}) allowed on leave."));
            }

            current = current.AddDays(1);
        }

        return Task.FromResult(LeaveValidationResult.Pass());
    }
}
