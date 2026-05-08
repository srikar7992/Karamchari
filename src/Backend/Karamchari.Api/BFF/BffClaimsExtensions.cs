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
}
