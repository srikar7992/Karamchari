namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines the rules for a specific type of leave (e.g., Sick, Casual, Privilege).
/// Uses EF Core JSON mapping for flexible rule storage.
/// </summary>
public sealed class LeavePolicy : AggregateRoot<Guid>, ITenantOwned
{
    private LeavePolicy(Guid id, string name, string description, LeavePolicyRules rules) : base(id)
    {
        Name = name;
        Description = description;
        Rules = rules;
        IsActive = true;
    }

    private LeavePolicy()
    {
        Name = string.Empty;
        Description = string.Empty;
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
    /// Gets the description of the leave type.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the policy is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the rules associated with this policy (mapped to JSON).
    /// </summary>
    public LeavePolicyRules Rules { get; private set; } = null!;

    /// <summary>
    /// Creates a new leave policy.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="rules">The policy rules.</param>
    /// <returns>A new <see cref="LeavePolicy"/> instance.</returns>
    public static LeavePolicy Create(string name, string description, LeavePolicyRules rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(rules);
        return new LeavePolicy(Guid.NewGuid(), name.Trim(), description.Trim(), rules);
    }
}

/// <summary>
/// Defines the category of leave.
/// </summary>
public enum LeaveCategory
{
    /// <summary>Employee is paid during leave.</summary>
    Paid = 1,
    /// <summary>Employee is not paid during leave.</summary>
    Unpaid = 2,
    /// <summary>Legally mandated leave (e.g. Maternity).</summary>
    Statutory = 3
}

/// <summary>
/// Defines how leave is accrued.
/// </summary>
public enum AccrualFrequency
{
    /// <summary>Full allowance given at the start of the year.</summary>
    Upfront = 1,
    /// <summary>Portion of allowance given each month.</summary>
    Monthly = 2,
    /// <summary>Earned based on actual hours worked.</summary>
    HoursWorked = 3
}

/// <summary>
/// Represents the detailed rules for a leave policy, mapped to JSON in the database.
/// </summary>
public sealed class LeavePolicyRules
{
    /// <summary>Gets or sets the category of leave.</summary>
    public LeaveCategory Category { get; set; }
    /// <summary>Gets or sets the annual allowance in days.</summary>
    public double AnnualAllowance { get; set; }
    /// <summary>Gets or sets the frequency of accrual.</summary>
    public AccrualFrequency AccrualFrequency { get; set; }
    /// <summary>Gets or sets whether manager approval is required.</summary>
    public bool RequiresManagerApproval { get; set; }
    /// <summary>Gets or sets whether half-days are permitted.</summary>
    public bool AllowHalfDays { get; set; }
    /// <summary>Gets or sets the maximum days that can be carried forward.</summary>
    public double MaximumCarryForward { get; set; }
}
