// -----------------------------------------------------------------------
// <copyright file="GoalType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum GoalType
{
    /// <summary>Numeric target with a unit (e.g., "close 50 deals").</summary>
    Measurable = 1,
    /// <summary>Discrete milestone checklist.</summary>
    Milestone = 2,
    /// <summary>Binary done/not-done.</summary>
    Binary = 3
}
