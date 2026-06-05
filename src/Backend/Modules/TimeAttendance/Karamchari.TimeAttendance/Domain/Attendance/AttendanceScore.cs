using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.TimeAttendance.Domain.Attendance.Events;

namespace Karamchari.TimeAttendance.Domain.Attendance;

/// <summary>
/// Workforce reliability intelligence per employee.
/// One record per employee — upserted on each attendance finalization.
/// This is where attendance becomes business intelligence.
///
/// ReliabilityScore = Present / Assigned * 100
/// ConsistencyScore = (Assigned - Late) / Assigned * 100
/// ComplianceScore  = weighted deduction per violation type
/// </summary>
public sealed class AttendanceScore : AggregateRoot<Guid>, ITenantOwned
{
    private AttendanceScore() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    public decimal ReliabilityScore { get; private set; }
    public decimal ConsistencyScore { get; private set; }
    public decimal ComplianceScore { get; private set; }

    // Counters used for score computation (rolling window or all-time)
    public int TotalAssignedShifts { get; private set; }
    public int TotalPresentShifts { get; private set; }
    public int TotalLateArrivals { get; private set; }
    public int TotalNoShows { get; private set; }
    public int TotalMissingPunches { get; private set; }
    public int TotalUnauthorizedOvertime { get; private set; }
    public int TotalLocationViolations { get; private set; }

    public DateTimeOffset LastCalculated { get; private set; }

    // Compliance breakdown — shows exactly which violations cost points.
    // Compliance = 100 - (NoShows*5 + MissingPunches*2 + UnauthorizedOT*2 + LocationViolations*3)
    // Exposed so dashboards can show "you lost 15 points to no-shows" not just "Compliance: 85".
    public decimal NoShowDeductionPoints => TotalNoShows * 5m;
    public decimal MissingPunchDeductionPoints => TotalMissingPunches * 2m;
    public decimal OvertimeDeductionPoints => TotalUnauthorizedOvertime * 2m;
    public decimal LocationViolationDeductionPoints => TotalLocationViolations * 3m;

    public static AttendanceScore Create(string tenantId, Guid employeeId)
    {
        return new AttendanceScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ReliabilityScore = 100m,
            ConsistencyScore = 100m,
            ComplianceScore = 100m,
            LastCalculated = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Recalculates all three scores from raw counters.
    /// Edge case: zero assigned shifts → scores remain at 100 (new employee, no data yet).
    /// </summary>
    public void Recalculate(
        int totalAssigned,
        int totalPresent,
        int totalLate,
        int totalNoShows,
        int totalMissingPunches,
        int totalUnauthorizedOvertime,
        int totalLocationViolations)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalAssigned);
        ArgumentOutOfRangeException.ThrowIfNegative(totalPresent);
        ArgumentOutOfRangeException.ThrowIfNegative(totalLate);

        TotalAssignedShifts = totalAssigned;
        TotalPresentShifts = totalPresent;
        TotalLateArrivals = totalLate;
        TotalNoShows = totalNoShows;
        TotalMissingPunches = totalMissingPunches;
        TotalUnauthorizedOvertime = totalUnauthorizedOvertime;
        TotalLocationViolations = totalLocationViolations;

        if (totalAssigned == 0)
        {
            // No data yet — perfect scores, not a zero-division error
            ReliabilityScore = 100m;
            ConsistencyScore = 100m;
            ComplianceScore = 100m;
            LastCalculated = DateTimeOffset.UtcNow;
            return;
        }

        // Reliability: did employee show up?
        ReliabilityScore = Math.Round((decimal)totalPresent / totalAssigned * 100, 2);

        // Consistency: how often on time?
        // Late shifts reduce consistency, but present-and-late is still counted as "showed up"
        var onTimeShifts = totalPresent - Math.Min(totalLate, totalPresent);
        ConsistencyScore = Math.Round((decimal)onTimeShifts / totalAssigned * 100, 2);

        // Compliance: policy adherence
        // Deduction weights (per occurrence, capped at 0):
        //   NoShow: -5 points each
        //   MissingPunch: -2 points each
        //   UnauthorizedOvertime: -2 points each
        //   LocationViolation: -3 points each
        var deductions =
            (totalNoShows * 5m) +
            (totalMissingPunches * 2m) +
            (totalUnauthorizedOvertime * 2m) +
            (totalLocationViolations * 3m);

        ComplianceScore = Math.Max(0m, Math.Round(100m - deductions, 2));

        LastCalculated = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ReliabilityScoreUpdated(
            EmployeeId, TenantId, ReliabilityScore, ConsistencyScore, ComplianceScore));
    }

    /// <summary>
    /// Delta adjustment for a regularization approval.
    /// O(1) — adjusts stored counters without scanning all records.
    /// Correctness relies on counters being accurate; nightly RecalculateAsync provides
    /// a course-correction path if counters ever drift.
    /// </summary>
    public void ApplyRegularizationDelta(
        AttendanceStatus previousStatus,
        IReadOnlyList<AttendanceExceptionType> resolvedViolations)
    {
        // Undo the previous status contribution
        if (previousStatus == AttendanceStatus.Absent)
            TotalNoShows = Math.Max(0, TotalNoShows - 1);
        else if (previousStatus is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay)
            TotalPresentShifts = Math.Max(0, TotalPresentShifts - 1); // was already present-ish, still will be

        // Regularization always results in Present — add it back
        TotalPresentShifts++;

        // Undo resolved violation counters
        foreach (var violationType in resolvedViolations)
        {
            switch (violationType)
            {
                case AttendanceExceptionType.LateArrival:
                    TotalLateArrivals = Math.Max(0, TotalLateArrivals - 1);
                    break;
                case AttendanceExceptionType.MissingCheckIn:
                case AttendanceExceptionType.MissingCheckOut:
                    TotalMissingPunches = Math.Max(0, TotalMissingPunches - 1);
                    break;
                case AttendanceExceptionType.Overtime:
                    TotalUnauthorizedOvertime = Math.Max(0, TotalUnauthorizedOvertime - 1);
                    break;
                case AttendanceExceptionType.LocationViolation:
                    TotalLocationViolations = Math.Max(0, TotalLocationViolations - 1);
                    break;
                case AttendanceExceptionType.NoShow:
                    // NoShow is a status outcome already handled above via TotalNoShows
                    break;
            }
        }

        Recalculate(
            TotalAssignedShifts,
            TotalPresentShifts,
            TotalLateArrivals,
            TotalNoShows,
            TotalMissingPunches,
            TotalUnauthorizedOvertime,
            TotalLocationViolations);
    }

    /// <summary>
    /// Incremental update for a single finalized attendance record.
    /// More efficient than full recalculation for real-time updates.
    /// </summary>
    public void ApplyAttendanceResult(
        AttendanceStatus status,
        bool hadLateArrival,
        bool hadMissingPunch,
        bool hadUnauthorizedOvertime,
        bool hadLocationViolation)
    {
        TotalAssignedShifts++;

        if (status is AttendanceStatus.Present or AttendanceStatus.Late or AttendanceStatus.HalfDay)
            TotalPresentShifts++;

        if (status == AttendanceStatus.Absent)
            TotalNoShows++;

        if (hadLateArrival) TotalLateArrivals++;
        if (hadMissingPunch) TotalMissingPunches++;
        if (hadUnauthorizedOvertime) TotalUnauthorizedOvertime++;
        if (hadLocationViolation) TotalLocationViolations++;

        Recalculate(
            TotalAssignedShifts,
            TotalPresentShifts,
            TotalLateArrivals,
            TotalNoShows,
            TotalMissingPunches,
            TotalUnauthorizedOvertime,
            TotalLocationViolations);
    }
}
