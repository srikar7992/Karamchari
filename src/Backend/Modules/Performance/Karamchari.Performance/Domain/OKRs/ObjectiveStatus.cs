// -----------------------------------------------------------------------
// <copyright file="ObjectiveStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum ObjectiveStatus
{
    Draft = 1,
    Active = 2,
    AtRisk = 3,
    OffTrack = 4,
    Scored = 5,
    Cancelled = 6
}
