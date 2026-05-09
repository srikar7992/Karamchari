using System.Security.Claims;

namespace Karamchari.Api.BFF;

internal static class BffClaimsExtensions
{
    /// <summary>
    /// Extracts the employee ID from the JWT 'sub' claim.
    /// Dev-mode fallback: reads X-Employee-Id header (string UUID).
    /// Returns Guid.Empty if neither is present or parseable — callers
    /// must return 401 when the result is empty.
    /// </summary>
    internal static Guid GetEmployeeId(this ClaimsPrincipal user, HttpRequest request)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is not null && Guid.TryParse(sub, out var fromSub))
            return fromSub;

        if (request.Headers.TryGetValue("X-Employee-Id", out var header)
            && Guid.TryParse(header.ToString(), out var fromHeader))
            return fromHeader;

        return Guid.Empty;
    }

    /// <summary>
    /// Extracts the tenant ID from the JWT 'tenant_id' claim.
    /// Returns null if absent — callers must return 401.
    /// </summary>
    internal static string? GetTenantId(this ClaimsPrincipal user) =>
        user.FindFirstValue("tenant_id");

    internal static string? GetTenantId(this ClaimsPrincipal user, HttpRequest _) =>
        user.GetTenantId();

    internal static string? GetEmployeeIdString(this ClaimsPrincipal user, HttpRequest request)
    {
        var id = user.GetEmployeeId(request);
        return id == Guid.Empty ? null : id.ToString();
    }

    /// <summary>
    /// Extracts both tenant ID and employee ID (from 'sub' claim) in one call.
    /// Either value may be null/empty — callers must return 401 when that happens.
    /// </summary>
    internal static (string? TenantId, Guid? EmployeeId) GetTenantAndEmployee(this ClaimsPrincipal user)
    {
        var tenantId = user.FindFirstValue("tenant_id");
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? employeeId = Guid.TryParse(sub, out var parsed) ? parsed : null;
        return (tenantId, employeeId);
    }
}
