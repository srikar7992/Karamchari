// -----------------------------------------------------------------------
// <copyright file="BurnoutSignalInput.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Flat snapshot of all burnout-relevant signals for a single employee.
/// Constructed by WorkforceSignalService from raw WorkforceSignalRecords.
/// Passed to BurnoutScoreCalculator — no DB access inside the calculator.
/// </summary>
public sealed record BurnoutSignalInput(
    Guid EmployeeId,
    int ConsecutiveWorkDays,
    decimal OvertimeHours28d,
    int DaysWithoutLeave,
    int LateArrivalsCurrentMonth,
    int LateArrivalsPreviousMonth,
    int LateArrivalsMonthMinus2,
    int ShiftSwaps30d,
    decimal HighIntensityShiftRatio,
    int EmergencyFillIns90d,
    int DataAgeDays,
    bool IsNewJoiner,
    string ShiftCategory
);

/// <summary>Result produced by BurnoutScoreCalculator.</summary>
public sealed record BurnoutScoreResult(
    decimal Score,
    Karamchari.Intelligence.Domain.Workforce.WorkforceRiskLevel RiskLevel,
    Karamchari.Intelligence.Domain.Workforce.WorkforceScoreConfidence Confidence,
    Karamchari.Intelligence.Domain.Signals.ScoreExplanation Explanation,
    IReadOnlyDictionary<string, decimal> ComponentScores
);
