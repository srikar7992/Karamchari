using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Geo;

public enum GeoZoneType
{
    Radial,
    Polygon
}

/// <summary>
/// Defines a geographic attendance zone. Supports both circular (center + radius) and polygon (WKT) shapes.
/// Nested zones supported — the most specific matching zone (smallest area) is used for attribution.
/// </summary>
public sealed class GeoZone : AggregateRoot<Guid>, ITenantOwned
{
    private GeoZone() { }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;

    public GeoZoneType ZoneType { get; private set; }

    // Radial zone
    public double? CenterLatitude { get; private set; }
    public double? CenterLongitude { get; private set; }
    public int? RadiusMeters { get; private set; }

    // Grace buffer around the defined fence to absorb GPS drift (default +25m)
    public int GraceBufferMeters { get; private set; }

    // Polygon zone (GeoJSON or WKT)
    public string? PolygonWkt { get; private set; }

    public string Timezone { get; private set; } = "UTC";
    public bool IsActive { get; private set; }

    /// <summary>Optional parent zone ID for nested zone hierarchies (campus → building → floor).</summary>
    public Guid? ParentZoneId { get; private set; }

    /// <summary>Area in square meters — used to resolve the most specific matching zone.</summary>
    public double ApproximateAreaSquareMeters { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static GeoZone CreateRadial(
        string tenantId,
        string name,
        string label,
        double centerLatitude,
        double centerLongitude,
        int radiusMeters,
        string timezone = "UTC",
        int graceBufferMeters = 25,
        Guid? parentZoneId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusMeters);
        ArgumentOutOfRangeException.ThrowIfNegative(graceBufferMeters);

        int effectiveRadius = radiusMeters + graceBufferMeters;
        return new GeoZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Label = label.Trim(),
            ZoneType = GeoZoneType.Radial,
            CenterLatitude = centerLatitude,
            CenterLongitude = centerLongitude,
            RadiusMeters = radiusMeters,
            GraceBufferMeters = graceBufferMeters,
            Timezone = timezone,
            IsActive = true,
            ParentZoneId = parentZoneId,
            ApproximateAreaSquareMeters = Math.PI * effectiveRadius * effectiveRadius,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public static GeoZone CreatePolygon(
        string tenantId,
        string name,
        string label,
        string polygonWkt,
        double approximateAreaSquareMeters,
        string timezone = "UTC",
        int graceBufferMeters = 25,
        Guid? parentZoneId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(polygonWkt);

        return new GeoZone
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Label = label.Trim(),
            ZoneType = GeoZoneType.Polygon,
            PolygonWkt = polygonWkt,
            GraceBufferMeters = graceBufferMeters,
            Timezone = timezone,
            IsActive = true,
            ParentZoneId = parentZoneId,
            ApproximateAreaSquareMeters = approximateAreaSquareMeters,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Checks if a GPS point is inside this radial zone (including grace buffer).
    /// Uses Haversine formula for distance calculation.
    /// </summary>
    public bool ContainsPoint(double latitude, double longitude)
    {
        if (!IsActive) return false;

        if (ZoneType == GeoZoneType.Radial)
        {
            if (!CenterLatitude.HasValue || !CenterLongitude.HasValue || !RadiusMeters.HasValue)
                return false;

            double distance = HaversineDistanceMeters(CenterLatitude.Value, CenterLongitude.Value, latitude, longitude);
            return distance <= (RadiusMeters.Value + GraceBufferMeters);
        }

        // Polygon containment delegated to caller (requires spatial library).
        // GeoConfidenceScorer handles polygon via NetTopologySuite.
        return false;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
