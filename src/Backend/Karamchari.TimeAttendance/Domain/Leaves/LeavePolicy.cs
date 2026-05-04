namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines the rules for a specific type of leave (e.g., Sick, Casual, Privilege).
/// </summary>
public sealed class LeavePolicy : AggregateRoot<Guid>, ITenantOwned
{
    private LeavePolicy(Guid id, string name, double annualEntitlement, bool canCarryForward) : base(id)
    {
        Name = name;
        AnnualEntitlement = annualEntitlement;
        CanCarryForward = canCarryForward;
    }

    private LeavePolicy()
    {
        Name = string.Empty;
        TenantId = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the leave type.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the annual entitlement in days.
    /// </summary>
    public double AnnualEntitlement { get; private set; }

    /// <summary>
    /// Gets a value indicating whether unused leave can be carried forward to the next year.
    /// </summary>
    public bool CanCarryForward { get; private set; }

    /// <summary>
    /// Creates a new leave policy.
    /// </summary>
    /// <param name="name">The name of the leave type.</param>
    /// <param name="annualEntitlement">The annual entitlement in days.</param>
    /// <param name="canCarryForward">Whether leave can be carried forward.</param>
    /// <returns>A new <see cref="LeavePolicy"/> instance.</returns>
    public static LeavePolicy Create(string name, double annualEntitlement, bool canCarryForward)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(annualEntitlement);
        return new LeavePolicy(Guid.NewGuid(), name.Trim(), annualEntitlement, canCarryForward);
    }
}
