namespace Karamchari.Core.Caching.Tenant;

public interface ITenantContextAccessor
{
    TenantContext? TenantContext { get; }
}

public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string? TenantSchema { get; set; }
    public TenantSource Source { get; set; }
}

public enum TenantSource
{
    JwtClaim,
    TrustedHeader,
    Subdomain,
    Background,
    AdminOverride
}
