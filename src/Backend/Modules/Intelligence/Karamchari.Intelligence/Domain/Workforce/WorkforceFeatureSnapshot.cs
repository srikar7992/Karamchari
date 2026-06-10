// -----------------------------------------------------------------------
// <copyright file="WorkforceFeatureSnapshot.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Daily feature snapshot for ML training data collection.
/// Written by FeatureSnapshotJob every night after scores are recalculated.
/// Each row captures all current signal values for one employee on one date.
/// Becomes the feature matrix for future supervised learning models.
/// </summary>
public sealed class WorkforceFeatureSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid EmployeeId { get; private set; }

    public DateOnly SnapshotDate { get; private set; }

    // ── Burnout signals ───────────────────────────────────────────────────────
    public int ConsecutiveWorkDays { get; private set; }
    public decimal OvertimeHours28d { get; private set; }
    public int DaysWithoutLeave { get; private set; }
    public int LateArrivalsCurrentMonth { get; private set; }
    public int ShiftSwaps30d { get; private set; }
    public decimal HighIntensityShiftRatio { get; private set; }
    public int EmergencyFillIns90d { get; private set; }

    // ── Attrition signals ────────────────────────────────────────────────────
    public decimal LateArrivalSlope { get; private set; }
    public decimal LeaveFrequencyRatio { get; private set; }
    public decimal SickLeaveDays30d { get; private set; }
    public int OvertimeRejections30d { get; private set; }
    public decimal PeerAttendanceGap { get; private set; }
    public decimal ManagerFrictionScore { get; private set; }

    // ── Derived scores ───────────────────────────────────────────────────────
    public decimal BurnoutScore { get; private set; }
    public decimal AttritionScore { get; private set; }
    public decimal TalentRiskScore { get; private set; }

    public WorkforceRiskLevel BurnoutRiskLevel { get; private set; }
    public WorkforceRiskLevel AttritionRiskLevel { get; private set; }

    public DateTime CapturedAt { get; private set; }

    private WorkforceFeatureSnapshot() { }

    public static WorkforceFeatureSnapshot Capture(
        string tenantId,
        Guid employeeId,
        DateOnly snapshotDate,
        int consecutiveWorkDays,
        decimal overtimeHours28d,
        int daysWithoutLeave,
        int lateArrivalsCurrentMonth,
        int shiftSwaps30d,
        decimal highIntensityShiftRatio,
        int emergencyFillIns90d,
        decimal lateArrivalSlope,
        decimal leaveFrequencyRatio,
        decimal sickLeaveDays30d,
        int overtimeRejections30d,
        decimal peerAttendanceGap,
        decimal managerFrictionScore,
        decimal burnoutScore,
        decimal attritionScore,
        decimal talentRiskScore,
        WorkforceRiskLevel burnoutRiskLevel,
        WorkforceRiskLevel attritionRiskLevel)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SnapshotDate = snapshotDate,
            ConsecutiveWorkDays = consecutiveWorkDays,
            OvertimeHours28d = overtimeHours28d,
            DaysWithoutLeave = daysWithoutLeave,
            LateArrivalsCurrentMonth = lateArrivalsCurrentMonth,
            ShiftSwaps30d = shiftSwaps30d,
            HighIntensityShiftRatio = highIntensityShiftRatio,
            EmergencyFillIns90d = emergencyFillIns90d,
            LateArrivalSlope = lateArrivalSlope,
            LeaveFrequencyRatio = leaveFrequencyRatio,
            SickLeaveDays30d = sickLeaveDays30d,
            OvertimeRejections30d = overtimeRejections30d,
            PeerAttendanceGap = peerAttendanceGap,
            ManagerFrictionScore = managerFrictionScore,
            BurnoutScore = burnoutScore,
            AttritionScore = attritionScore,
            TalentRiskScore = talentRiskScore,
            BurnoutRiskLevel = burnoutRiskLevel,
            AttritionRiskLevel = attritionRiskLevel,
            CapturedAt = DateTime.UtcNow
        };
}
