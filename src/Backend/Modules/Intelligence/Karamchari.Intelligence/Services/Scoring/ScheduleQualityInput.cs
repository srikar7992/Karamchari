namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Inputs for <see cref="ScheduleQualityCalculator"/>.
/// All values sourced from <c>WorkforceSignalRecord</c> rows.
/// </summary>
/// <param name="NightToMorningTransitions7d">Count of night-shift followed by morning-shift in the same rolling 7-day window.</param>
/// <param name="ShortRestViolations7d">Count of shift pairings with less than 11 hours rest gap in the rolling 7-day window.</param>
/// <param name="WeekendShiftClustering">Fraction of rolling 4-week windows where both Saturday and Sunday were worked [0–1].</param>
/// <param name="SplitShiftCount30d">Count of calendar days with two non-contiguous shift blocks (split shifts) in rolling 30 days.</param>
public sealed record ScheduleQualityInput(
    int NightToMorningTransitions7d,
    int ShortRestViolations7d,
    decimal WeekendShiftClustering,
    int SplitShiftCount30d
);
