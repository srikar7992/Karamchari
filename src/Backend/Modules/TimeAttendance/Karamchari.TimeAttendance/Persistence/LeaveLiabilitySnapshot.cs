using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

/// <summary>
/// Monthly financial provisioning snapshot consumed by Finance for leave liability reporting.
/// Answers: "How much leave liability (in days and monetary value) do we owe this month?"
/// Computed from LeaveBalance, CarryForward, and payroll cost data.
/// Never mutated — a new snapshot row is created each month.
/// </summary>
public sealed class LeaveLiabilitySnapshot : ITenantOwned
{
    private LeaveLiabilitySnapshot() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? LocationId { get; private set; }

    // Aggregate leave days
    public decimal TotalEarnedLeaveBalanceDays { get; private set; }
    public decimal TotalCarryForwardDays { get; private set; }
    public decimal TotalEncashableBalanceDays { get; private set; }
    public decimal TotalLopDaysYtd { get; private set; }

    // Monetary estimates
    public decimal EstimatedLiabilityAmount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;

    public int EmployeeCount { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }
    public string ComputedBy { get; private set; } = string.Empty;

    public static LeaveLiabilitySnapshot Create(
        string tenantId,
        int year,
        int month,
        Guid? departmentId,
        Guid? locationId,
        decimal earnedBalanceDays,
        decimal carryForwardDays,
        decimal encashableBalanceDays,
        decimal lopDaysYtd,
        decimal estimatedLiabilityAmount,
        string currencyCode,
        int employeeCount,
        string computedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(computedBy);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        return new LeaveLiabilitySnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Year = year,
            Month = month,
            DepartmentId = departmentId,
            LocationId = locationId,
            TotalEarnedLeaveBalanceDays = earnedBalanceDays,
            TotalCarryForwardDays = carryForwardDays,
            TotalEncashableBalanceDays = encashableBalanceDays,
            TotalLopDaysYtd = lopDaysYtd,
            EstimatedLiabilityAmount = estimatedLiabilityAmount,
            CurrencyCode = currencyCode,
            EmployeeCount = employeeCount,
            ComputedAt = DateTimeOffset.UtcNow,
            ComputedBy = computedBy,
        };
    }
}
