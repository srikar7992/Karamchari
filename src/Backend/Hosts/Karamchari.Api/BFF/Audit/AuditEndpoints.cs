using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Audit;

/// <summary>
/// Forensic audit trail queries. Powered by AuditInterceptor which captures before/after values
/// at the EF layer for every entity change across all bounded contexts.
/// </summary>
public static class AuditEndpoints
{
    public static WebApplication MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/audit").RequireAuthorization();

        group.MapGet("/trail", QueryAuditTrail).WithName("Audit.Trail");
        group.MapGet("/entity/{tableName}/{primaryKey}", GetEntityHistory).WithName("Audit.Entity.History");
        group.MapGet("/actors/{userId}", GetActorHistory).WithName("Audit.Actor.History");

        return app;
    }

    /// <summary>
    /// Full audit trail with optional filters.
    /// Returns: who, what table, what action, when, before values, after values.
    /// </summary>
    private static async Task<IResult> QueryAuditTrail(
        [FromQuery] string? tableName,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        ClaimsPrincipal user = default!,
        CoreDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();
        if (pageSize > 200) pageSize = 200;

        var query = db.AuditLogs.Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(a => a.TableName == tableName);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
            query = query.Where(a => a.TimestampUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.TimestampUtc <= to.Value);

        var total = await query.CountAsync(ct);
        var entries = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.TableName,
                a.Action,
                a.UserId,
                a.TimestampUtc,
                a.PrimaryKey,
                a.OldValues,
                a.NewValues,
            })
            .ToListAsync(ct);

        return Results.Ok(new { Total = total, Page = page, PageSize = pageSize, Data = entries });
    }

    /// <summary>
    /// Full change history for a specific entity record.
    /// Provides the immutable audit chain: every state the entity was ever in.
    /// </summary>
    private static async Task<IResult> GetEntityHistory(
        string tableName,
        string primaryKey,
        ClaimsPrincipal user,
        CoreDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var history = await db.AuditLogs
            .Where(a => a.TenantId == tenantId
                && a.TableName == tableName
                && a.PrimaryKey.Contains(primaryKey))
            .OrderBy(a => a.TimestampUtc)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.UserId,
                a.TimestampUtc,
                a.OldValues,
                a.NewValues,
            })
            .ToListAsync(ct);

        return Results.Ok(new { TableName = tableName, PrimaryKey = primaryKey, History = history });
    }

    /// <summary>
    /// All changes made by a specific user/actor across all entities.
    /// Useful for user activity audits and security investigations.
    /// </summary>
    private static async Task<IResult> GetActorHistory(
        string userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        ClaimsPrincipal user = default!,
        CoreDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.AuditLogs
            .Where(a => a.TenantId == tenantId && a.UserId == userId);

        if (from.HasValue) query = query.Where(a => a.TimestampUtc >= from.Value);
        if (to.HasValue) query = query.Where(a => a.TimestampUtc <= to.Value);

        var total = await query.CountAsync(ct);
        var entries = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * 50)
            .Take(50)
            .Select(a => new
            {
                a.Id,
                a.TableName,
                a.Action,
                a.TimestampUtc,
                a.PrimaryKey,
                a.OldValues,
                a.NewValues,
            })
            .ToListAsync(ct);

        return Results.Ok(new { UserId = userId, Total = total, Page = page, Data = entries });
    }
}
