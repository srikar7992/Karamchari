using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Geo;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class GeoZoneTests
{
    private const string TenantId = "tenant-geo-1";

    [Fact]
    public void ContainsPoint_returns_true_when_inside_radius()
    {
        // Zone centered at (12.9716, 77.5946) — Bengaluru center, 200m radius
        var zone = GeoZone.CreateRadial(TenantId, "Office", "Main Office",
            centerLatitude: 12.9716, centerLongitude: 77.5946,
            radiusMeters: 200, graceBufferMeters: 25);

        // Point 100m away — inside
        var inside = zone.ContainsPoint(12.9725, 77.5946);
        inside.Should().BeTrue();
    }

    [Fact]
    public void ContainsPoint_returns_false_when_far_outside_radius()
    {
        var zone = GeoZone.CreateRadial(TenantId, "Office", "Main Office",
            centerLatitude: 12.9716, centerLongitude: 77.5946,
            radiusMeters: 100, graceBufferMeters: 25);

        // Point ~2km away
        var outside = zone.ContainsPoint(12.9900, 77.5946);
        outside.Should().BeFalse();
    }

    [Fact]
    public void ContainsPoint_respects_grace_buffer()
    {
        // Zone 100m radius + 25m grace = 125m effective
        var zone = GeoZone.CreateRadial(TenantId, "Office", "Main Office",
            centerLatitude: 12.9716, centerLongitude: 77.5946,
            radiusMeters: 100, graceBufferMeters: 25);

        // Point at ~115m — outside strict radius but inside grace buffer
        var borderPoint = zone.ContainsPoint(12.9726, 77.5946);
        borderPoint.Should().BeTrue();
    }

    [Fact]
    public void Inactive_zone_never_contains_any_point()
    {
        var zone = GeoZone.CreateRadial(TenantId, "Office", "Main Office",
            centerLatitude: 12.9716, centerLongitude: 77.5946,
            radiusMeters: 500);

        zone.Deactivate();

        zone.ContainsPoint(12.9716, 77.5946).Should().BeFalse(); // center itself
    }

    [Fact]
    public void CreateRadial_computes_approximate_area()
    {
        var zone = GeoZone.CreateRadial(TenantId, "Zone", "Z",
            12.0, 77.0, radiusMeters: 100, graceBufferMeters: 0);

        // Area = π × r² = π × 100² ≈ 31415
        zone.ApproximateAreaSquareMeters.Should().BeApproximately(Math.PI * 10000, 1.0);
    }

    [Fact]
    public void CreateRadial_with_negative_radius_throws()
    {
        var act = () => GeoZone.CreateRadial(TenantId, "Bad", "B", 12.0, 77.0, radiusMeters: -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreatePolygon_zone_created_correctly()
    {
        var wkt = "POLYGON((77.0 12.0, 77.1 12.0, 77.1 12.1, 77.0 12.1, 77.0 12.0))";
        var zone = GeoZone.CreatePolygon(TenantId, "Campus", "HQ Campus",
            wkt, approximateAreaSquareMeters: 1_000_000);

        zone.ZoneType.Should().Be(GeoZoneType.Polygon);
        zone.PolygonWkt.Should().Be(wkt);
        zone.IsActive.Should().BeTrue();
    }
}
