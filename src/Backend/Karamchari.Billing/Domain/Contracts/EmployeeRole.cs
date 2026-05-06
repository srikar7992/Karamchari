namespace Karamchari.Billing.Domain.Contracts;

/// <summary>
/// Maps an employee to a specific billing role (e.g., "Senior Architect").
/// This mapping is used by the Billing engine to resolve the correct rate from a Contract.
/// </summary>
public sealed class EmployeeRole
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;

    public Guid EmployeeId { get; init; }
    public Guid RoleId { get; init; }

    /// <summary>Optional: If null, this is the default role for the employee across all projects.</summary>
    public Guid? ProjectId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
}
