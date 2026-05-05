namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines a valid geographical boundary for mobile attendance punches.
/// </summary>
public sealed class GeoFence : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    /// <summary>The allowed radius in meters from the center point.</summary>
    public double RadiusMeters { get; private set; }

    private GeoFence() { }

    public static GeoFence Create(string tenantId, string name, double latitude, double longitude, double radiusMeters)
    {
        return new GeoFence
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Latitude = latitude,
            Longitude = longitude,
            RadiusMeters = radiusMeters
        };
    }
}
