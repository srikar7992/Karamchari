namespace Karamchari.Core.Contracts;

/// <summary>
/// Event published when a new tenant has been provisioned and their physical schema created.
/// </summary>
/// <param name="TenantId">The unique identifier for the tenant (e.g. "sch_oakridge").</param>
/// <param name="CompanyName">The name of the company.</param>
/// <param name="AdminEmail">The email address of the primary administrator.</param>
public record TenantProvisionedIntegrationEvent(string TenantId, string CompanyName, string AdminEmail);
