using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Persistent read model for leave balances.
/// Implements Priority 1 Step 15.
/// </summary>
public sealed class LeaveBalanceReadModel : ITenantOwned
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public Guid PolicyId { get; init; }
    public string PolicyName { get; init; } = string.Empty;

    public decimal AvailableBalance { get; set; }
    public decimal ConsumedBalance { get; set; }

    public DateTimeOffset LastUpdatedAtUtc { get; set; }
    public string Version { get; set; } = "1.0";
}
