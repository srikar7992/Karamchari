namespace Karamchari.TimeAttendance.Domain.Timesheets;

/// <summary>
/// Domain service for cross-entry timesheet validation and normalisation.
/// All methods are pure / static — no I/O, no DI dependencies.
/// </summary>
public static class TimesheetValidator
{
    // ── Normalisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Splits a single entry that spans midnight into two or more entries,
    /// one per calendar day (employee-local date derived from <see cref="TimeEntry.Date"/>).
    ///
    /// Example: 10 PM → 2 AM  produces:
    ///   Day 1: 22:00 – 00:00  (2 h)
    ///   Day 2: 00:00 – 02:00  (2 h)
    ///
    /// If the entry has no UTC timestamps, or does not cross midnight, it is
    /// returned unchanged in a single-element list.
    /// </summary>
    public static IReadOnlyList<TimeEntry> NormalizeAcrossMidnight(TimeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!entry.StartTimeUtc.HasValue || !entry.EndTimeUtc.HasValue)
            return [entry];

        var start = entry.StartTimeUtc.Value;
        var end = entry.EndTimeUtc.Value;

        // Same calendar day — nothing to split.
        if (start.Date == end.Date)
            return [entry];

        var results = new List<TimeEntry>();
        var cursor = start;

        // Walk day-by-day until we reach the end date.
        while (cursor.Date < end.Date)
        {
            var midnight = cursor.Date.AddDays(1); // UTC midnight ending the current day
            var segmentHours = Math.Round((decimal)(midnight - cursor).TotalHours, 2);

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
        var finalHours = Math.Round((decimal)(end - cursor).TotalHours, 2);
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

    // ── Cross-entry validation ───────────────────────────────────────────────

    /// <summary>
    /// Validates a full set of entries for daily limits and overlapping UTC windows.
    /// Must be called after <see cref="NormalizeAcrossMidnight"/> so split segments
    /// do not false-positive on the same-day check.
    /// </summary>
    public static void ValidateEntries(IEnumerable<TimeEntry> entries)
    {
        var list = entries.ToList();

        // 1. Daily total ≤ 24 h per calendar date.
        foreach (var day in list.GroupBy(e => e.Date))
        {
            var total = day.Sum(e => e.Hours);
            if (total > 24)
                throw new InvalidOperationException(
                    $"Total hours for {day.Key:d} ({total}h) exceeds the 24-hour daily limit.");
        }

        // 2. Overlapping UTC windows — only entries with explicit timestamps.
        var timed = list
            .Where(e => e.StartTimeUtc.HasValue && e.EndTimeUtc.HasValue)
            .OrderBy(e => e.StartTimeUtc)
            .ToList();

        for (var i = 0; i < timed.Count - 1; i++)
        {
            var a = timed[i];
            var b = timed[i + 1];

            if (b.StartTimeUtc < a.EndTimeUtc)
                throw new InvalidOperationException(
                    $"Overlapping entries: '{a.Description}' ({a.StartTimeUtc:t}–{a.EndTimeUtc:t}) " +
                    $"overlaps with '{b.Description}' ({b.StartTimeUtc:t}–{b.EndTimeUtc:t}).");
        }
    }

    // ── Capacity validation ──────────────────────────────────────────────────

    /// <summary>
    /// Ensures total billable hours do not exceed the employee's contracted weekly capacity.
    /// No-op when <paramref name="maxBillableHours"/> is null (no cap configured).
    /// </summary>
    public static void ValidateCapacity(IEnumerable<TimeEntry> entries, decimal? maxBillableHours)
    {
        if (maxBillableHours is null)
            return;

        var billable = entries.Where(e => e.IsBillable).Sum(e => e.Hours);
        if (billable > maxBillableHours.Value)
            throw new InvalidOperationException(
                $"Billable hours ({billable}h) exceed assigned weekly capacity ({maxBillableHours}h). " +
                "Reduce billable hours or request a capacity increase from your project manager.");
    }
}
