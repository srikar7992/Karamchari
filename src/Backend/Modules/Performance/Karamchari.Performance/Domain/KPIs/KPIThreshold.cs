// -----------------------------------------------------------------------
// <copyright file="KPIThreshold.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Owned value object â€” defines Red/Yellow/Green band boundaries for a KPI.
/// For HigherIsBetter: value &lt; RedBelow â†’ Red, &lt; YellowBelow â†’ Yellow, else Green.
/// For LowerIsBetter: value &gt; RedAbove â†’ Red, &gt; YellowAbove â†’ Yellow, else Green.
/// </summary>
public sealed record KPIThreshold(
    decimal RedBelow,
    decimal YellowBelow,
    decimal GreenAbove)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public KPIBand Evaluate(decimal value, KPIPolarity polarity)
    {
        if (polarity == KPIPolarity.HigherIsBetter)
        {
            if (value < RedBelow) return KPIBand.Red;
            if (value < YellowBelow) return KPIBand.Yellow;
            return KPIBand.Green;
        }
        else
        {
            if (value > RedBelow) return KPIBand.Red;
            if (value > YellowBelow) return KPIBand.Yellow;
            return KPIBand.Green;
        }
    }
}
