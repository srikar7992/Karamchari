namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Explicit transition table for LeaveRequest state machine.
/// Centralizes all allowed transitions. Illegal transitions throw at call site.
///
/// Legal flow:
///   Draft → Submitted | Cancelled
///   Submitted → PendingApproval | Approved | Withdrawn | Expired
///   PendingApproval → Approved | Rejected | Expired
///   Approved → Cancelled
///   Rejected | Cancelled | Withdrawn | Expired → (terminal, no transitions)
/// </summary>
public static class LeaveStateMachine
{
    private static readonly Dictionary<LeaveRequestStatus, HashSet<LeaveRequestStatus>> Transitions = new()
    {
        [LeaveRequestStatus.Draft] =
            [LeaveRequestStatus.Submitted, LeaveRequestStatus.Cancelled],

        [LeaveRequestStatus.Submitted] =
            [LeaveRequestStatus.PendingApproval, LeaveRequestStatus.Approved,
             LeaveRequestStatus.Withdrawn, LeaveRequestStatus.Expired],

        [LeaveRequestStatus.PendingApproval] =
            [LeaveRequestStatus.Approved, LeaveRequestStatus.Rejected, LeaveRequestStatus.Expired],

        [LeaveRequestStatus.Approved] =
            [LeaveRequestStatus.Cancelled],

        [LeaveRequestStatus.Rejected] = [],
        [LeaveRequestStatus.Cancelled] = [],
        [LeaveRequestStatus.Withdrawn] = [],
        [LeaveRequestStatus.Expired] = []
    };

    public static bool CanTransition(LeaveRequestStatus from, LeaveRequestStatus to)
        => Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Throws InvalidOperationException for illegal transitions.</summary>
    public static void Guard(LeaveRequestStatus from, LeaveRequestStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"Leave request cannot transition from {from} to {to}.");
    }

    public static IReadOnlySet<LeaveRequestStatus> AllowedTransitionsFrom(LeaveRequestStatus status)
        => Transitions.TryGetValue(status, out var targets) ? targets : new HashSet<LeaveRequestStatus>();
}
