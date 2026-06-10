// -----------------------------------------------------------------------
// <copyright file="ArrearPeriodDiff.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Arrears;

/// <summary>
/// Period-level differential: original vs recalculated amounts for one payroll period.
/// Immutable value â€” recalculate rather than mutate.
/// </summary>
public sealed record ArrearPeriodDiff(
    string PeriodName,
    int Year,
    int Month,
    decimal OriginalGross,
    decimal RecalculatedGross,
    decimal OriginalNet,
    decimal RecalculatedNet,
    decimal OriginalTds,
    decimal RecalculatedTds,
    IReadOnlyDictionary<string, decimal> ComponentDeltas)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GrossDelta => RecalculatedGross - OriginalGross;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NetDelta => RecalculatedNet - OriginalNet;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TdsDelta => RecalculatedTds - OriginalTds;
}
