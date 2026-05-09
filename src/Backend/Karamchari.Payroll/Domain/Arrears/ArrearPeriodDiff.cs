namespace Karamchari.Payroll.Domain.Arrears;

/// <summary>
/// Period-level differential: original vs recalculated amounts for one payroll period.
/// Immutable value — recalculate rather than mutate.
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
    public decimal GrossDelta => RecalculatedGross - OriginalGross;
    public decimal NetDelta => RecalculatedNet - OriginalNet;
    public decimal TdsDelta => RecalculatedTds - OriginalTds;
}
