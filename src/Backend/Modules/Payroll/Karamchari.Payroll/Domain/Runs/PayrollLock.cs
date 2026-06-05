using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Runs;

/// <summary>
/// Explicit immutable lock record. Once created, payroll run cannot be edited or recalculated.
/// </summary>
public sealed class PayrollLock : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public DateTimeOffset LockedAt { get; private set; }
    public string LockedBy { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;

    private PayrollLock() { }

    public static PayrollLock Create(string tenantId, Guid payrollRunId, string lockedBy, string reason = "")
    {
        return new PayrollLock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            LockedBy = lockedBy,
            Reason = reason,
            LockedAt = DateTimeOffset.UtcNow
        };
    }
}
