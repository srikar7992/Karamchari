// -----------------------------------------------------------------------
// <copyright file="ReviewCycleStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ReviewCycleStatus
{
    Draft = 1,
    Enrolling = 2,
    InProgress = 3,
    Calibrating = 4,
    Completed = 5,
    Archived = 6
}
