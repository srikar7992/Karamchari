namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Value object representing a contiguous period between two timestamps.
/// Enforces Start &lt; End. Immutable.
/// </summary>
public sealed record TimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>Gets the duration of the time range.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>Checks if this time range overlaps with another.</summary>
    /// <param name="other">The other time range.</param>
    /// <returns>True if they overlap, otherwise false.</returns>
    public bool Overlaps(TimeRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Start < other.End && other.Start < End;
    }

    /// <summary>Checks if the time range contains a specific point in time.</summary>
    /// <param name="point">The point in time.</param>
    /// <returns>True if contained, otherwise false.</returns>
    public bool Contains(DateTimeOffset point) => point >= Start && point <= End;

    /// <summary>Creates a new TimeRange.</summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    /// <returns>A validated time range.</returns>
    public static TimeRange Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after start.");
        return new TimeRange(start, end);
    }
}

/// <summary>
/// Value object representing geographic coordinates.
/// </summary>
public sealed record GeoPoint(double Latitude, double Longitude)
{
    /// <summary>Creates a new GeoPoint.</summary>
    /// <param name="lat">The latitude.</param>
    /// <param name="lng">The longitude.</param>
    /// <returns>A validated GeoPoint.</returns>
    public static GeoPoint Create(double lat, double lng)
    {
        if (lat is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(lat));
        if (lng is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(lng));
        return new GeoPoint(lat, lng);
    }
}
