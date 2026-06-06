namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Flat snapshot of all attrition-relevant signals for a single employee.
/// Constructed by WorkforceSignalService from raw WorkforceSignalRecords.
/// </summary>
public sealed record AttritionSignalInput(
    Guid EmployeeId,
    decimal LateArrivalSlope,
    decimal LeaveFrequencyRatio,
    decimal SickLeaveDays30d,
    decimal HistoricalSickLeaveBaseline,
    int ShiftSwaps30d,
    int OvertimeRejections30d,
    decimal PeerAttendanceGap,
    decimal ManagerFrictionScore,
    int DataAgeDays,
    bool IsNewJoiner
);

/// <summary>Result produced by AttritionScoreCalculator.</summary>
public sealed record AttritionScoreResult(
    decimal Score,
    Karamchari.Intelligence.Domain.Workforce.WorkforceRiskLevel RiskLevel,
    Karamchari.Intelligence.Domain.Workforce.WorkforceScoreConfidence Confidence,
    Karamchari.Intelligence.Domain.Signals.ScoreExplanation Explanation,
    IReadOnlyDictionary<string, decimal> ComponentScores
);
