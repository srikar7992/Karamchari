// -----------------------------------------------------------------------
// <copyright file="TimesheetValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Domain service for cross-entry timesheet validation and normalisation.
/// All methods are pure / static â€” no I/O, no DI dependencies.
/// </summary>
public static class TimesheetValidator
{
    // â”€â”€ Normalisation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Splits a single entry that spans midnight into two or more entries,
    /// one per calendar day (employee-local date derived from <see cref="TimeEntry.Date"/>).
    ///
    /// Example: 10 PM â†’ 2 AM  produces:
    ///   Day 1: 22:00 â€“ 00:00  (2 h)
    ///   Day 2: 00:00 â€“ 02:00  (2 h)
    ///
    /// If the entry has no UTC timestamps, or does not cross midnight, it is
    /// returned unchanged in a single-element list.
    /// </summary>
    public static IReadOnlyList<TimeEntry> NormalizeAcrossMidnight(TimeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!entry.StartTimeUtc.HasValue || !entry.EndTimeUtc.HasValue)
            return [entry];

        DateTime start = entry.StartTimeUtc.Value;
        DateTime end = entry.EndTimeUtc.Value;

        // Same calendar day â€” nothing to split.
        if (start.Date == end.Date)
            return [entry];

        var results = new List<TimeEntry>();
        DateTime cursor = start;

        // Walk day-by-day until we reach the end date.
        while (cursor.Date < end.Date)
        {
            DateTime midnight = cursor.Date.AddDays(1); // UTC midnight ending the current day
            decimal segmentHours = Math.Round((decimal)(midnight - cursor).TotalHours, 2);

            results.Add(entry with
            {
                EntryId = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(cursor),
                StartTimeUtc = cursor,
                EndTimeUtc = midnight,
                Hours = segmentHours,
            });

            cursor = midnight;
        }

        // Final segment on the last day.
        decimal finalHours = Math.Round((decimal)(end - cursor).TotalHours, 2);
        if (finalHours > 0)
        {
            results.Add(entry with
            {
                EntryId = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(cursor),
                StartTimeUtc = cursor,
                EndTimeUtc = end,
                Hours = finalHours,
            });
        }

        return results;
    }

    // â”€â”€ Cross-entry validation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Validates a full set of entries for daily limits and overlapping UTC windows.
    /// Must be called after <see cref="NormalizeAcrossMidnight"/> so split segments
    /// do not false-positive on the same-day check.
    /// </summary>
    public static void ValidateEntries(IEnumerable<TimeEntry> entries)
    {
        var list = entries.ToList();

        // 1. Daily total â‰¤ 24 h per calendar date.
        foreach (IGrouping<DateOnly, TimeEntry> day in list.GroupBy(e => e.Date))
        {
            decimal total = day.Sum(e => e.Hours);
            if (total > 24)
                throw new InvalidOperationException(
                    $"Total hours for {day.Key:d} ({total}h) exceeds the 24-hour daily limit.");
        }

        // 2. Overlapping UTC windows â€” only entries with explicit timestamps.
        var timed = list
            .Where(e => e.StartTimeUtc.HasValue && e.EndTimeUtc.HasValue)
            .OrderBy(e => e.StartTimeUtc)
            .ToList();

        for (int i = 0; i < timed.Count - 1; i++)
        {
            TimeEntry a = timed[i];
            TimeEntry b = timed[i + 1];

            if (b.StartTimeUtc < a.EndTimeUtc)
                throw new InvalidOperationException(
                    $"Overlapping entries: '{a.Description}' ({a.StartTimeUtc:t}â€“{a.EndTimeUtc:t}) " +
                    $"overlaps with '{b.Description}' ({b.StartTimeUtc:t}â€“{b.EndTimeUtc:t}).");
        }
    }

    // â”€â”€ Capacity validation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Ensures total billable hours do not exceed the employee's contracted weekly capacity.
    /// No-op when <paramref name="maxBillableHours"/> is null (no cap configured).
    /// </summary>
    public static void ValidateCapacity(IEnumerable<TimeEntry> entries, decimal? maxBillableHours)
    {
        if (maxBillableHours is null)
            return;

        decimal billable = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
        if (billable > maxBillableHours.Value)
            throw new InvalidOperationException(
                $"Billable hours ({billable}h) exceed assigned weekly capacity ({maxBillableHours}h). " +
                "Reduce billable hours or request a capacity increase from your project manager.");
    }
}
