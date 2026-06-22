namespace Karamchari.Analytics.Domain;

public sealed class DimDate
{
    public int DateKey { get; init; }
    public DateOnly Date { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public int Quarter { get; init; }
    public int WeekOfYear { get; init; }
    public bool IsWeekend { get; init; }
}
