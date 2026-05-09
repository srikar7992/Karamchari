namespace Karamchari.Core.Domain.Primitives;

/// <summary>
/// Value object representing a contiguous period between two timestamps.
/// Enforces Start < End. Immutable.
/// </summary>
public sealed record TimeRange(DateTimeOffset Start, DateTimeOffset End)
{
    public TimeSpan Duration => End - Start;

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;

    public bool Contains(DateTimeOffset point) => point >= Start && point <= End;

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
    public static GeoPoint Create(double lat, double lng)
    {
        if (lat is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(lat));
        if (lng is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(lng));
        return new GeoPoint(lat, lng);
    }
}
