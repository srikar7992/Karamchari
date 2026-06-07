using FluentAssertions;
using Karamchari.TimeAttendance.Domain.Biometric;
using Xunit;

namespace Karamchari.TimeAttendance.Tests;

public sealed class BiometricDeviceTests
{
    private const string TenantId = "tenant-device-1";

    private static BiometricDevice RegisterDevice(string serial = "SN-001") =>
        BiometricDevice.Register(TenantId, serial, "Suprema", "BioStation 3", "v3.2.1",
            "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----");

    [Fact]
    public void Register_creates_device_in_pending_registration_state()
    {
        var device = RegisterDevice();

        device.Status.Should().Be(DeviceStatus.PendingRegistration);
        device.TamperDetected.Should().BeFalse();
        device.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Activate_sets_status_to_active()
    {
        var device = RegisterDevice();
        device.Activate();

        device.Status.Should().Be(DeviceStatus.Active);
    }

    [Fact]
    public void RecordHeartbeat_updates_last_heartbeat_and_recovers_offline()
    {
        var device = RegisterDevice();
        device.Activate();
        device.MarkOffline();
        device.Status.Should().Be(DeviceStatus.Offline);

        device.RecordHeartbeat();

        device.Status.Should().Be(DeviceStatus.Active);
        device.LastHeartbeatUtc.Should().NotBeNull();
    }

    [Fact]
    public void FlagTamper_sets_tampered_status()
    {
        var device = RegisterDevice();
        device.Activate();
        device.FlagTamper();

        device.Status.Should().Be(DeviceStatus.Tampered);
        device.TamperDetected.Should().BeTrue();
    }

    [Fact]
    public void AssignZone_sets_zone_metadata()
    {
        var device = RegisterDevice();
        device.Activate();

        var zoneId = Guid.NewGuid();
        device.AssignZone(zoneId, "Building A");

        device.ZoneId.Should().Be(zoneId);
        device.ZoneName.Should().Be("Building A");
    }

    [Fact]
    public void AssignZone_on_decommissioned_device_throws()
    {
        var device = RegisterDevice();
        device.Activate();
        device.Decommission();

        var act = () => device.AssignZone(Guid.NewGuid(), "Zone");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decommission_twice_throws()
    {
        var device = RegisterDevice();
        device.Activate();
        device.Decommission();

        var act = () => device.Decommission();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsStale_returns_true_when_heartbeat_exceeded_timeout()
    {
        var device = RegisterDevice();
        device.Activate();
        device.RecordHeartbeat();

        // Heartbeat just recorded — not stale with 1h timeout
        device.IsStale(TimeSpan.FromHours(1)).Should().BeFalse();

        // Stale with 0 timeout (edge case: timeout = 0)
        device.IsStale(TimeSpan.Zero).Should().BeTrue();
    }

    [Fact]
    public void UpdateFirmware_updates_version()
    {
        var device = RegisterDevice();
        device.UpdateFirmware("v3.3.0");

        device.FirmwareVersion.Should().Be("v3.3.0");
    }

    [Fact]
    public void Decommission_clears_zone_assignment()
    {
        var device = RegisterDevice();
        device.Activate();
        device.AssignZone(Guid.NewGuid(), "Zone A");
        device.Decommission();

        device.ZoneId.Should().BeNull();
        device.Status.Should().Be(DeviceStatus.Decommissioned);
    }

    [Fact]
    public void Register_with_empty_serial_throws()
    {
        var act = () => BiometricDevice.Register(TenantId, "", "Vendor", "Model", "v1", "cert");
        act.Should().Throw<ArgumentException>();
    }
}
