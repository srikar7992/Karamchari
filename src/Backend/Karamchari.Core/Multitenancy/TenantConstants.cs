namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Centralized constants and validation rules for multi-tenancy.
/// </summary>
public static class TenantConstants
{
    /// <summary>
    /// Regex pattern for valid TenantId: lowercase alphanumeric, hyphens, or underscores.
    /// Starts with alphanumeric. 1-50 characters.
    /// </summary>
    public const string TenantIdPattern = "^[a-z0-9][a-z0-9_-]{0,49}$";

    /// <summary>
    /// Max length for TenantId in database columns.
    /// </summary>
    public const int MaxTenantIdLength = 64;

    /// <summary>
    /// The claim type used for the tenant identifier in JWTs.
    /// </summary>
    public const string JwtClaimType = "tenant_id";

    /// <summary>
    /// The HTTP header name used for tenant identification in service-to-service calls.
    /// </summary>
    public const string HeaderName = "X-Tenant-Id";

    /// <summary>
    /// The HTTP header name used for correlation identification.
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";
}
