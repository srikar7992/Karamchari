using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Karamchari.Api.BFF.ESS;

/// <summary>
/// Endpoint filter that prevents cross-employee data access on the ESS /me/* surface.
/// Extracts the employee ID from JWT claims and stamps it into HttpContext.Items so
/// handlers can read it without re-parsing claims on every call.
///
/// Usage: .AddEndpointFilter&lt;EmployeeScopeGuard&gt;() on the ESS /me/* group.
/// </summary>
public sealed class EmployeeScopeGuard : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
            return Results.Unauthorized();

        var employeeIdClaim = user.FindFirst("employee_id")
            ?? user.FindFirst(ClaimTypes.NameIdentifier);

        if (employeeIdClaim == null || !Guid.TryParse(employeeIdClaim.Value, out var employeeId))
            return Results.Forbid();

        context.HttpContext.Items["AuthenticatedEmployeeId"] = employeeId;

        return await next(context);
    }
}

public static class EmployeeScopeGuardExtensions
{
    /// <summary>Returns the employee ID stamped by <see cref="EmployeeScopeGuard"/>.</summary>
    public static Guid GetScopedEmployeeId(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("AuthenticatedEmployeeId", out var value) && value is Guid id)
            return id;

        throw new InvalidOperationException(
            "EmployeeScopeGuard must be applied to this endpoint group before calling GetScopedEmployeeId.");
    }
}
