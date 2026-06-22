using Karamchari.Core.Security;
using Karamchari.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Karamchari.Api.BFF.Governance;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit").RequireAuthorization();

        group.MapGet("/entries", QueryAuditEntries)
             .RequireAuthorization(Permissions.AuditRead);
    }

    private static async Task<IResult> QueryAuditEntries(
        string? entityType,
        string? entityId,
        Guid? actorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditQueryService svc,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId))
            return Results.Unauthorized();

        var entries = await svc.QueryAsync(tenantId, entityType, entityId, actorId, from, to, ct);
        return Results.Ok(entries);
    }
}
