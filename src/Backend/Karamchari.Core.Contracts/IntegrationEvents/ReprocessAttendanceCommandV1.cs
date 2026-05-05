namespace Karamchari.Core.Contracts.IntegrationEvents;

/// <summary>
/// Command to trigger bulk or single attendance reprocessing.
/// Version 1.
/// </summary>
public sealed record ReprocessAttendanceCommandV1
{
    public Guid JobId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public DateTime FromDateUtc { get; init; }
    public DateTime ToDateUtc { get; init; }
}
