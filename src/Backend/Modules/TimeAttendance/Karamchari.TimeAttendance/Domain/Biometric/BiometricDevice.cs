using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Biometric;

/// <summary>
/// Tracks the lifecycle of a physical biometric device from registration through decommission.
/// Each device has an HSM-backed key pair injected at manufacture; the certificate is pinned on tenant registration.
/// </summary>
public sealed class BiometricDevice : AggregateRoot<Guid>, ITenantOwned
{
    private BiometricDevice() { }

    public string TenantId { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string Vendor { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string FirmwareVersion { get; private set; } = string.Empty;

    /// <summary>Device public key certificate (PEM). Used for mTLS and signed-result verification.</summary>
    public string CertificatePem { get; private set; } = string.Empty;

    public DeviceStatus Status { get; private set; }

    /// <summary>Zone/location this device is assigned to. Determines which employee templates sync down.</summary>
    public Guid? ZoneId { get; private set; }

    public string? ZoneName { get; private set; }
    public string? BuildingFloorGate { get; private set; }

    public DateTimeOffset RegisteredAtUtc { get; private set; }
    public DateTimeOffset? LastHeartbeatUtc { get; private set; }
    public DateTimeOffset? DecommissionedAtUtc { get; private set; }

    /// <summary>Running count of failed sensor readings — tracked for dirty-sensor alerts.</summary>
    public int SensorQualityFailCount { get; private set; }

    /// <summary>True when device health attestation failed on last sync (tamper or root).</summary>
    public bool TamperDetected { get; private set; }

    public static BiometricDevice Register(
        string tenantId,
        string serialNumber,
        string vendor,
        string model,
        string firmwareVersion,
        string certificatePem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePem);

        return new BiometricDevice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SerialNumber = serialNumber.Trim().ToUpperInvariant(),
            Vendor = vendor.Trim(),
            Model = model.Trim(),
            FirmwareVersion = firmwareVersion.Trim(),
            CertificatePem = certificatePem,
            Status = DeviceStatus.PendingRegistration,
            RegisteredAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Activate() => Status = DeviceStatus.Active;

    public void AssignZone(Guid zoneId, string zoneName, string? buildingFloorGate = null)
    {
        if (Status == DeviceStatus.Decommissioned)
            throw new InvalidOperationException("Cannot assign zone to a decommissioned device.");

        ZoneId = zoneId;
        ZoneName = zoneName.Trim();
        BuildingFloorGate = buildingFloorGate?.Trim();
    }

    public void RecordHeartbeat()
    {
        if (Status == DeviceStatus.Offline)
            Status = DeviceStatus.Active;

        LastHeartbeatUtc = DateTimeOffset.UtcNow;
    }

    public void MarkOffline() => Status = DeviceStatus.Offline;

    public void RecordSensorQualityFailure()
    {
        SensorQualityFailCount++;
    }

    public void FlagTamper()
    {
        TamperDetected = true;
        Status = DeviceStatus.Tampered;
    }

    public void UpdateFirmware(string newVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);
        FirmwareVersion = newVersion.Trim();
    }

    public void Decommission()
    {
        if (Status == DeviceStatus.Decommissioned)
            throw new InvalidOperationException("Device already decommissioned.");

        Status = DeviceStatus.Decommissioned;
        DecommissionedAtUtc = DateTimeOffset.UtcNow;
        ZoneId = null;
        ZoneName = null;
        // Certificate is revoked externally via CRL; we mark status here.
    }

    public bool IsStale(TimeSpan heartbeatTimeout)
        => LastHeartbeatUtc.HasValue && DateTimeOffset.UtcNow - LastHeartbeatUtc.Value > heartbeatTimeout;
}
