namespace Karamchari.TimeAttendance.Domain.IoT;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a hardware biometric device or a mobile endpoint authorized to send punches.
/// </summary>
public sealed class Device : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier of the device record.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the physical hardware MAC address or serial number.</summary>
    public string DeviceId { get; private set; } = string.Empty;

    /// <summary>Gets the API Key used by this device to authenticate edge requests.</summary>
    public string ApiKey { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the device is active and allowed to push data.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets an optional location description.</summary>
    public string? Location { get; private set; }

    private Device() { }

    public static Device Create(string tenantId, string deviceId, string apiKey, string? location)
    {
        return new Device
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeviceId = deviceId,
            ApiKey = apiKey,
            IsActive = true,
            Location = location
        };
    }
}
