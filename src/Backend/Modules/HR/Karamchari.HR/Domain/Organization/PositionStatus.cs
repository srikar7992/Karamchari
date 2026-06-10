// -----------------------------------------------------------------------
// <copyright file="PositionStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Defines the lifecycle status of a position slot.
/// </summary>
public enum PositionStatus
{
    /// <summary>The position is a draft and not yet approved.</summary>
    Draft,
    /// <summary>The position is approved but not yet active or advertised.</summary>
    Approved,
    /// <summary>The position is active and open for candidates/assignment.</summary>
    Open,
    /// <summary>The position is occupied by an active employee assignment.</summary>
    Filled,
    /// <summary>The position has been frozen, suspending recruitment or assignment.</summary>
    Frozen,
    /// <summary>The position is closed or deactivated permanently.</summary>
    Closed
}
