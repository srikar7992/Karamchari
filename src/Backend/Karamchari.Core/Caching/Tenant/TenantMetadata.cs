namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Represents metadata for a tenant.
/// </summary>
public sealed record TenantMetadata(
    string TenantId,
    string Name,
    string Plan,
    bool IsActive,
    DateTime CreatedAt
);
