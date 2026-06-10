// -----------------------------------------------------------------------
// <copyright file="TaxRegime.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines the available income tax regimes in India.
/// </summary>
public enum TaxRegime
{
    /// <summary>The traditional regime with exemptions and deductions.</summary>
    Old = 0,

    /// <summary>The new simplified regime with lower rates but fewer exemptions (Default).</summary>
    New = 1
}
