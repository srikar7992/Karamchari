namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Event published when a live attendance state changes (e.g., check-in, check-out).
/// Used to decouple the high-throughput ingestion consumer from the SignalR broadcaster.
/// Version 1.
/// </summary>
public sealed record LiveAttendanceUpdatedEventV1
{
    /// <summary>The tenant identifier.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The employee whose attendance state changed.</summary>
    public Guid EmployeeId { get; init; }

    /// <summary>True when the employee is currently checked in.</summary>
    public bool IsPresent { get; init; }

    /// <summary>Human-readable status label (e.g. "Present", "Absent", "OnLeave").</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>UTC timestamp of the most recent check-in. Null if never checked in.</summary>
    public DateTime? LastCheckIn { get; init; }

    /// <summary>UTC timestamp of the most recent check-out. Null if still checked in.</summary>
    public DateTime? LastCheckOut { get; init; }

    /// <summary>UTC timestamp when this record was last updated.</summary>
    public DateTime LastUpdatedAtUtc { get; init; }
}
