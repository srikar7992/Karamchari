using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Policy;

public enum PolicyScope
{
    Organisation,
    BusinessUnit,
    Department,
    ShiftType,
    Individual
}

/// <summary>
/// Attendance policy with hierarchical override support.
/// Resolution order (most specific wins): Organisation → BusinessUnit → Department → ShiftType → Individual.
/// All time values are in minutes. All boolean flags default to disabled.
/// </summary>
public sealed class AttendancePolicyHierarchy : AggregateRoot<Guid>, ITenantOwned
{
    private AttendancePolicyHierarchy() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PolicyScope Scope { get; private set; }

    /// <summary>Null for Organisation scope. BU/dept/shift/employee ID for narrower scopes.</summary>
    public Guid? ScopeEntityId { get; private set; }

    // --- Module 4: Configurable rules ---

    /// <summary>Minutes after shift start before punch is marked Late. 0 = no grace (strict).</summary>
    public int LateGraceMinutes { get; private set; }

    /// <summary>
    /// If late by more than this many minutes, mark HalfDay (first half).
    /// Typically: arrival after 50% of shift elapsed.
    /// </summary>
    public int LateToHalfDayMinutes { get; private set; }

    /// <summary>Minimum minutes worked for full-day credit.</summary>
    public int FullDayMinMinutes { get; private set; }

    /// <summary>Minimum minutes worked for half-day credit.</summary>
    public int HalfDayMinMinutes { get; private set; }

    /// <summary>Minutes early departure before EarlyOut exception is raised.</summary>
    public int EarlyOutToleranceMinutes { get; private set; }

    /// <summary>Number of EarlyOut occurrences per month that trigger a leave deduction.</summary>
    public int EarlyOutLeaveDeductionThreshold { get; private set; }

    /// <summary>Flat break minutes auto-deducted from raw hours (when no break punch exists).</summary>
    public int AutoBreakDeductionMinutes { get; private set; }

    // --- Overtime ---

    /// <summary>Daily worked minutes after which OT is triggered.</summary>
    public int DailyOtTriggerMinutes { get; private set; }

    /// <summary>Weekly worked minutes after which OT is triggered.</summary>
    public int WeeklyOtTriggerMinutes { get; private set; }

    /// <summary>Max OT per day in minutes (caps OT payout/comp-off).</summary>
    public int MaxDailyOtMinutes { get; private set; }

    /// <summary>OT multiplier for weekdays (e.g. 1.5 = time-and-a-half).</summary>
    public decimal WeekdayOtMultiplier { get; private set; }

    /// <summary>OT multiplier for public holidays.</summary>
    public decimal HolidayOtMultiplier { get; private set; }

    public bool RequireManagerOtApproval { get; private set; }

    // --- Comp-off ---

    /// <summary>
    /// Holiday/weekend work exceeding this many minutes accrues a comp-off day.
    /// 0 = comp-off disabled.
    /// </summary>
    public int CompOffAccrualTriggerMinutes { get; private set; }

    /// <summary>Days before an unused comp-off grant expires.</summary>
    public int CompOffExpiryDays { get; private set; }

    /// <summary>Whether expired comp-off is converted to encashment instead of forfeited.</summary>
    public bool CompOffPayableOnExpiry { get; private set; }

    // --- Absence ---

    /// <summary>Consecutive absent days (excluding approved leave) before HR escalation.</summary>
    public int ConsecutiveAbsenceEscalationDays { get; private set; }

    /// <summary>Whether absent days auto-deduct casual leave before going to LOP.</summary>
    public bool AutoDeductLeaveOnAbsent { get; private set; }

    // --- Dedup ---

    /// <summary>Window in seconds: duplicate punch from same employee/device within window is dropped.</summary>
    public int PunchDedupWindowSeconds { get; private set; }

    /// <summary>Max device clock skew in minutes before punch is flagged for review.</summary>
    public int MaxClockSkewMinutes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Creates the organisation-wide default policy.</summary>
    public static AttendancePolicyHierarchy CreateOrgDefault(string tenantId, string name)
    {
        return new AttendancePolicyHierarchy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Scope = PolicyScope.Organisation,
            ScopeEntityId = null,
            LateGraceMinutes = 15,
            LateToHalfDayMinutes = 120,
            FullDayMinMinutes = 480,  // 8h
            HalfDayMinMinutes = 240,  // 4h
            EarlyOutToleranceMinutes = 15,
            EarlyOutLeaveDeductionThreshold = 3,
            AutoBreakDeductionMinutes = 0,
            DailyOtTriggerMinutes = 480,  // 8h
            WeeklyOtTriggerMinutes = 2400, // 40h
            MaxDailyOtMinutes = 240,       // 4h
            WeekdayOtMultiplier = 1.5m,
            HolidayOtMultiplier = 2.0m,
            RequireManagerOtApproval = true,
            CompOffAccrualTriggerMinutes = 240, // 4h holiday/weekend work
            CompOffExpiryDays = 60,
            CompOffPayableOnExpiry = false,
            ConsecutiveAbsenceEscalationDays = 3,
            AutoDeductLeaveOnAbsent = true,
            PunchDedupWindowSeconds = 60,
            MaxClockSkewMinutes = 5,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Creates a scoped override policy (BU/dept/shift/individual).</summary>
    public static AttendancePolicyHierarchy CreateOverride(
        string tenantId,
        string name,
        PolicyScope scope,
        Guid scopeEntityId,
        AttendancePolicyHierarchy basePolicy)
    {
        ArgumentNullException.ThrowIfNull(basePolicy);

        var copy = new AttendancePolicyHierarchy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            // Copy all values from base — caller mutates via Update() to override only what differs
            LateGraceMinutes = basePolicy.LateGraceMinutes,
            LateToHalfDayMinutes = basePolicy.LateToHalfDayMinutes,
            FullDayMinMinutes = basePolicy.FullDayMinMinutes,
            HalfDayMinMinutes = basePolicy.HalfDayMinMinutes,
            EarlyOutToleranceMinutes = basePolicy.EarlyOutToleranceMinutes,
            EarlyOutLeaveDeductionThreshold = basePolicy.EarlyOutLeaveDeductionThreshold,
            AutoBreakDeductionMinutes = basePolicy.AutoBreakDeductionMinutes,
            DailyOtTriggerMinutes = basePolicy.DailyOtTriggerMinutes,
            WeeklyOtTriggerMinutes = basePolicy.WeeklyOtTriggerMinutes,
            MaxDailyOtMinutes = basePolicy.MaxDailyOtMinutes,
            WeekdayOtMultiplier = basePolicy.WeekdayOtMultiplier,
            HolidayOtMultiplier = basePolicy.HolidayOtMultiplier,
            RequireManagerOtApproval = basePolicy.RequireManagerOtApproval,
            CompOffAccrualTriggerMinutes = basePolicy.CompOffAccrualTriggerMinutes,
            CompOffExpiryDays = basePolicy.CompOffExpiryDays,
            CompOffPayableOnExpiry = basePolicy.CompOffPayableOnExpiry,
            ConsecutiveAbsenceEscalationDays = basePolicy.ConsecutiveAbsenceEscalationDays,
            AutoDeductLeaveOnAbsent = basePolicy.AutoDeductLeaveOnAbsent,
            PunchDedupWindowSeconds = basePolicy.PunchDedupWindowSeconds,
            MaxClockSkewMinutes = basePolicy.MaxClockSkewMinutes,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        return copy;
    }

    public void Update(
        int lateGraceMinutes,
        int lateToHalfDayMinutes,
        int fullDayMinMinutes,
        int halfDayMinMinutes,
        int earlyOutToleranceMinutes,
        int earlyOutLeaveDeductionThreshold,
        int autoBreakDeductionMinutes,
        int dailyOtTriggerMinutes,
        int weeklyOtTriggerMinutes,
        int maxDailyOtMinutes,
        decimal weekdayOtMultiplier,
        decimal holidayOtMultiplier,
        bool requireManagerOtApproval,
        int compOffAccrualTriggerMinutes,
        int compOffExpiryDays,
        bool compOffPayableOnExpiry,
        int consecutiveAbsenceEscalationDays,
        bool autoDeductLeaveOnAbsent,
        int punchDedupWindowSeconds,
        int maxClockSkewMinutes)
    {
        if (halfDayMinMinutes >= fullDayMinMinutes)
            throw new ArgumentException("HalfDayMinMinutes must be less than FullDayMinMinutes.");
        ArgumentOutOfRangeException.ThrowIfLessThan(weekdayOtMultiplier, 1.0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(holidayOtMultiplier, 1.0m);

        LateGraceMinutes = lateGraceMinutes;
        LateToHalfDayMinutes = lateToHalfDayMinutes;
        FullDayMinMinutes = fullDayMinMinutes;
        HalfDayMinMinutes = halfDayMinMinutes;
        EarlyOutToleranceMinutes = earlyOutToleranceMinutes;
        EarlyOutLeaveDeductionThreshold = earlyOutLeaveDeductionThreshold;
        AutoBreakDeductionMinutes = autoBreakDeductionMinutes;
        DailyOtTriggerMinutes = dailyOtTriggerMinutes;
        WeeklyOtTriggerMinutes = weeklyOtTriggerMinutes;
        MaxDailyOtMinutes = maxDailyOtMinutes;
        WeekdayOtMultiplier = weekdayOtMultiplier;
        HolidayOtMultiplier = holidayOtMultiplier;
        RequireManagerOtApproval = requireManagerOtApproval;
        CompOffAccrualTriggerMinutes = compOffAccrualTriggerMinutes;
        CompOffExpiryDays = compOffExpiryDays;
        CompOffPayableOnExpiry = compOffPayableOnExpiry;
        ConsecutiveAbsenceEscalationDays = consecutiveAbsenceEscalationDays;
        AutoDeductLeaveOnAbsent = autoDeductLeaveOnAbsent;
        PunchDedupWindowSeconds = punchDedupWindowSeconds;
        MaxClockSkewMinutes = maxClockSkewMinutes;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Determines final attendance status given worked minutes and late arrival minutes.
    /// Edge cases: exactly at threshold uses the more favorable outcome.
    /// </summary>
    public AttendanceFinalStatus ComputeStatus(int workedMinutes, int lateByMinutes, bool isHolidayOrWeekend)
    {
        if (isHolidayOrWeekend)
            return workedMinutes > 0 ? AttendanceFinalStatus.HolidayWork : AttendanceFinalStatus.WeekOff;

        if (workedMinutes <= 0)
            return AttendanceFinalStatus.Absent;

        if (workedMinutes < HalfDayMinMinutes)
            return AttendanceFinalStatus.Absent;

        if (workedMinutes < FullDayMinMinutes)
            return AttendanceFinalStatus.HalfDay;

        if (lateByMinutes > LateToHalfDayMinutes)
            return AttendanceFinalStatus.HalfDay;

        if (lateByMinutes > LateGraceMinutes)
            return AttendanceFinalStatus.Late;

        return AttendanceFinalStatus.Present;
    }

    /// <summary>OT minutes capped at policy maximum.</summary>
    public int ComputeOvertimeMinutes(int workedMinutes)
    {
        int raw = Math.Max(0, workedMinutes - DailyOtTriggerMinutes);
        return Math.Min(raw, MaxDailyOtMinutes);
    }

    public bool ShouldAccrueCompOff(int workedMinutes)
        => CompOffAccrualTriggerMinutes > 0 && workedMinutes >= CompOffAccrualTriggerMinutes;
}

public enum AttendanceFinalStatus
{
    Present,
    Late,
    HalfDay,
    Absent,
    HolidayWork,
    WeekOff
}
