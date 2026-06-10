// -----------------------------------------------------------------------
// <copyright file="KPIResultStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum KPIResultStatus
{
    Computed = 1,
    ManualOverride = 2,
    Locked = 3
}
