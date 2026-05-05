namespace Karamchari.TimeAttendance.Domain.IoT;

public enum InvalidPunchStatus
{
    Pending = 1,
    Mapped = 2,
    Reprocessing = 3,
    Reprocessed = 4,
    Failed = 5,
    Closed = 6
}

/// <summary>
/// Dead Letter Queue table for raw punches that could not be parsed,
/// mapped, or validated (e.g. unknown BiometricId, malformed JSON).
/// Ensures zero data loss.
/// </summary>
public sealed class InvalidPunch
{
    public Guid Id { get; private set; }

    /// <summary>The raw, unparsed payload from the edge API.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>The reason this payload was rejected (e.g. "UnmappedBiometricId").</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The hardware device ID that submitted the payload, if parseable.</summary>
    public string? DeviceId { get; private set; }

    /// <summary>When the invalid payload was captured.</summary>
    public DateTime TimestampUtc { get; private set; }
    
    public InvalidPunchStatus Status { get; private set; }

    private InvalidPunch() { }

    public static InvalidPunch Create(string payload, string reason, string? deviceId)
    {
        return new InvalidPunch
        {
            Id = Guid.NewGuid(),
            Payload = payload,
            Reason = reason,
            DeviceId = deviceId,
            TimestampUtc = DateTime.UtcNow,
            Status = InvalidPunchStatus.Pending
        };
    }

    public void TransitionTo(InvalidPunchStatus newStatus)
    {
        // Add basic state machine validations if necessary
        Status = newStatus;
    }
}
