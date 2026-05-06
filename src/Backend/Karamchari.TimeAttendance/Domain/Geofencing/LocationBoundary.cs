using Karamchari.Core.Multitenancy;
using NetTopologySuite.Geometries;

namespace Karamchari.TimeAttendance.Domain.Geofencing;

/// <summary>
/// Represents a geographical boundary (geofence) for a specific site or office.
/// </summary>
public sealed class LocationBoundary : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The geographical center of the boundary.
    /// </summary>
    public Point Location { get; private set; } = default!;

    /// <summary>
    /// The radius of the boundary in meters.
    /// </summary>
    public double RadiusInMeters { get; private set; }

    public bool IsActive { get; private set; }

    private LocationBoundary() { }

    public static LocationBoundary Create(string tenantId, string name, double latitude, double longitude, double radiusInMeters)
    {
        return new LocationBoundary
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Location = new Point(longitude, latitude) { SRID = 4326 }, // WGS 84
            RadiusInMeters = radiusInMeters,
            IsActive = true
        };
    }
}
