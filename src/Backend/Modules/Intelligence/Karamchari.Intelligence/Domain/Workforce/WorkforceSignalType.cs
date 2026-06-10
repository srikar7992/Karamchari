// -----------------------------------------------------------------------
// <copyright file="WorkforceSignalType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
    CriticalShiftCoverageRatio, // fraction of critical shifts that only this employee can fill (0–1)

    // ── Phase 6.3: Dependency Risk signals ───────────────────────────────────
    CrossTrainingDepth,         // number of distinct roles the employee can cover (0–10+)
    UniqueRoleFlag,             // 1 if sole practitioner; 0 otherwise
    ApprovalAuthorityIndex,     // 0=none, 1=minor, 2=major, 3=exclusive approvals held

    // ── Phase 6.3: Schedule Quality signals ──────────────────────────────────
    NightToMorningTransitions7d, // night-shift followed by morning-shift in same 7-day window (count)
    ShortRestViolations7d,       // shifts with less than 11h rest gap preceding them (count)
    WeekendShiftClustering,      // fraction of rolling 4-week windows where both Sat + Sun were worked (0–1)
    SplitShiftCount30d           // shifts split across two blocks in the same calendar day (count)
}
