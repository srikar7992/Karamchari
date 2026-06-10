// -----------------------------------------------------------------------
// <copyright file="ScoreTrend.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>Direction of a workforce score over time.</summary>
public enum ScoreTrend
{
    Improving,
    Stable,
    Deteriorating
}

/// <summary>Rate of change for a workforce risk score.</summary>
/// <param name="PointsPerDay">Least-squares slope of recent scores (positive = worsening).</param>
/// <param name="WindowDays">Number of calendar days the slope was computed over.</param>
/// <param name="Trend">Bucketed direction derived from slope.</param>
/// <param name="TotalChangeInWindow">Score delta from first to last observation in the window.</param>
public sealed record ScoreVelocity(
    decimal PointsPerDay,
    int WindowDays,
    ScoreTrend Trend,
    decimal TotalChangeInWindow);
