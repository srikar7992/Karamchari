namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// All signal types that feed the burnout and attrition scoring engines.
/// Each value maps to a specific measurement captured from upstream modules.
/// </summary>
public enum WorkforceSignalType
{
    // ── Burnout signals ──────────────────────────────────────────────────────
    ConsecutiveWorkDays,       // current unbroken shift streak (count)
    OvertimeHours28d,          // overtime hours in rolling 28-day window
    DaysWithoutLeave,          // days elapsed since last approved leave
    LateArrivalsMonthly,       // late arrivals recorded in current calendar month
    ShiftSwaps30d,             // shift swaps/changes in rolling 30-day window
    HighIntensityShiftRatio,   // fraction of recent shifts classified as night/weekend/holiday (0–1)
    EmergencyFillIns90d,       // times used as emergency coverage gap-fill in 90-day window

    // ── Attrition signals ────────────────────────────────────────────────────
    LateArrivalSlope,          // regression slope of late arrivals over 3 months (positive = worsening)
    LeaveFrequencyRatio,       // recent leave requests / historical baseline (>1 = spike)
    SickLeaveDays30d,          // sick leave days consumed in rolling 30-day window
    ShiftSwapAttritionScore,   // qualitative: shift-away pattern weight (computed from swap direction)
    OvertimeRejections30d,     // overtime offers declined in rolling 30-day window
    PeerAttendanceGap,         // employee attendance % minus team average % (negative = below team)
    ManagerFrictionScore,      // normalised count of transfer requests + shift appeals + escalations (90d)

    // ── Phase 6.1 signals ────────────────────────────────────────────────────
    CriticalShiftCoverageRatio  // fraction of critical shifts that only this employee can fill (0–1)
}
