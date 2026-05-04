namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Tracks the available leave balance for an employee for a specific policy.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveBalance(Guid id, Guid employeeId, Guid policyId, double totalEntitlement) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        TotalEntitlement = totalEntitlement;
        UsedBalance = 0;
    }

    private LeaveBalance()
    {
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the employee identifier.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the leave policy identifier.
    /// </summary>
    public Guid PolicyId { get; private set; }

    /// <summary>
    /// Gets the total entitlement for the period.
    /// </summary>
    public double TotalEntitlement { get; private set; }

    /// <summary>
    /// Gets the number of leave days already used.
    /// </summary>
    public double UsedBalance { get; private set; }

    /// <summary>
    /// Gets the remaining leave balance.
    /// </summary>
    public double RemainingBalance => TotalEntitlement - UsedBalance;

    /// <summary>
    /// Creates a new leave balance for an employee.
    /// </summary>
    /// <param name="employeeId">The employee identifier.</param>
    /// <param name="policyId">The policy identifier.</param>
    /// <param name="totalEntitlement">The total entitlement.</param>
    /// <returns>A new <see cref="LeaveBalance"/> instance.</returns>
    public static LeaveBalance Create(Guid employeeId, Guid policyId, double totalEntitlement)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalEntitlement);
        return new LeaveBalance(Guid.NewGuid(), employeeId, policyId, totalEntitlement);
    }

    /// <summary>
    /// Deducts leave days from the balance.
    /// </summary>
    /// <param name="days">The number of days to deduct.</param>
    public void Deduct(double days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);
        if (days > RemainingBalance) throw new InvalidOperationException("Insufficient leave balance.");
        UsedBalance += days;
    }
}
