using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Schedules;

/// <summary>
/// Aggregate root for a physical or logical work site.
/// Owns the timezone context for all employees assigned here.
/// </summary>
public sealed class WorkforceLocation : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "UTC"; // IANA/Windows ID
    public GeoPoint? Center { get; private set; }
    public double GeofenceRadiusMeters { get; private set; }

    private WorkforceLocation() { }

    public static WorkforceLocation Create(string tenantId, string name, string tzId, GeoPoint? center = null, double radius = 100)
    {
        return new WorkforceLocation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            TimeZoneId = tzId,
            Center = center,
            GeofenceRadiusMeters = radius
        };
    }

    public void UpdateTimeZone(string tzId) => TimeZoneId = tzId;
}
