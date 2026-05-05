namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Event published when a live attendance state changes (e.g., check-in, check-out).
/// Used to decouple the high-throughput ingestion consumer from the SignalR broadcaster.
/// Version 1.
/// </summary>
public sealed record LiveAttendanceUpdatedEventV1
{
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public bool IsPresent { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? LastCheckIn { get; init; }
    public DateTime? LastCheckOut { get; init; }
    public DateTime LastUpdatedAtUtc { get; init; }
}
