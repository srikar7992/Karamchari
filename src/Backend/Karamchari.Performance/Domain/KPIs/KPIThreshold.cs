namespace Karamchari.Performance.Domain.KPIs;

/// <summary>
/// Owned value object — defines Red/Yellow/Green band boundaries for a KPI.
/// For HigherIsBetter: value &lt; RedBelow → Red, &lt; YellowBelow → Yellow, else Green.
/// For LowerIsBetter: value &gt; RedAbove → Red, &gt; YellowAbove → Yellow, else Green.
/// </summary>
public sealed record KPIThreshold(
    decimal RedBelow,
    decimal YellowBelow,
    decimal GreenAbove)
{
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
