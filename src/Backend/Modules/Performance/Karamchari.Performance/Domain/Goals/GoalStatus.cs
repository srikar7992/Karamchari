// -----------------------------------------------------------------------
// <copyright file="GoalStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Goals;

/// <summary>
/// Authoritative business status for goals.
/// </summary>
public enum GoalStatus
{
    /// <summary>Goal is being drafted or awaiting coordination.</summary>
    Draft = 1,

    /// <summary>Goal is active and being tracked.</summary>
    Active = 2,

    /// <summary>Goal has been completed.</summary>
    Completed = 3,

    /// <summary>Goal was discarded without completion.</summary>
    Discarded = 4,

    /// <summary>Goal has been closed (final score recorded).</summary>
    Closed = 5,

    /// <summary>Goal was rejected during coordination.</summary>
    Rejected = 6,

    /// <summary>Goal was revised (historical version).</summary>
    Revised = 7
}
