// -----------------------------------------------------------------------
// <copyright file="KRType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.OKRs;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum KRType
{
    /// <summary>Numeric target (e.g., revenue, NPS, count).</summary>
    Metric = 1,
    /// <summary>Checklist of milestone items.</summary>
    Milestone = 2,
    /// <summary>Binary done/not-done.</summary>
    Binary = 3
}
