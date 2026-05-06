namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Command to trigger bulk or single attendance reprocessing.
/// Version 1.
/// </summary>
public sealed record ReprocessAttendanceCommandV1
{
    /// <summary>Unique identifier for this reprocessing job (also the idempotency key).</summary>
    public Guid JobId { get; init; }

    /// <summary>The tenant whose attendance data will be reprocessed.</summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The specific employee to reprocess. Use <see cref="Guid.Empty"/> for all employees.</summary>
    public Guid EmployeeId { get; init; }

    /// <summary>Inclusive UTC start of the reprocessing window.</summary>
    public DateTime FromDateUtc { get; init; }

    /// <summary>Inclusive UTC end of the reprocessing window.</summary>
    public DateTime ToDateUtc { get; init; }
}
