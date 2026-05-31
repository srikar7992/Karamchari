namespace Karamchari.TimeAttendance.Services;

/// <summary>
/// Default capacity provider that imposes no billable-hour ceiling.
/// Replace with a real implementation once assignment data is available
/// (e.g., querying a project-assignment table for contracted hours per week).
/// </summary>
public sealed class NullCapacityProvider : ICapacityProvider
{
    public Task<decimal?> GetBillableCapacityAsync(
        Guid employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
        => Task.FromResult<decimal?>(null);
}
