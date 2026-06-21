using Karamchari.Core.Security;
using Karamchari.Extensibility.Domain;
using Karamchari.Extensibility.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Extensibility;

public static class ExtensibilityEndpoints
{
    public static void MapExtensibilityEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/extensibility").RequireAuthorization();

        g.MapGet("/entities", ListEntityDefinitions).RequireAuthorization(Permissions.ExtensibilityRead);
        g.MapPost("/entities", CreateEntityDefinition).RequireAuthorization(Permissions.ExtensibilityAdmin);
        g.MapGet("/entities/{id:guid}", GetEntityDefinition).RequireAuthorization(Permissions.ExtensibilityRead);
        g.MapPost("/entities/{id:guid}/fields", AddField).RequireAuthorization(Permissions.ExtensibilityAdmin);
        g.MapDelete("/entities/{id:guid}", DeactivateEntity).RequireAuthorization(Permissions.ExtensibilityAdmin);
        g.MapGet("/entities/{id:guid}/records", ListRecords).RequireAuthorization(Permissions.ExtensibilityRead);
        g.MapPost("/entities/{id:guid}/records", CreateRecord).RequireAuthorization(Permissions.ExtensibilityRead);
        g.MapGet("/records/{id:guid}", GetRecord).RequireAuthorization(Permissions.ExtensibilityRead);
        g.MapPut("/records/{id:guid}", UpdateRecord).RequireAuthorization(Permissions.ExtensibilityRead);
    }

    private static async Task<IResult> ListEntityDefinitions(ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var defs = await db.EntityDefinitions.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .Select(d => new { d.Id, d.EntityName, d.Description, FieldCount = d.Fields.Count })
            .ToListAsync(ct);
        return Results.Ok(defs);
    }

    private static async Task<IResult> CreateEntityDefinition(
        [FromBody] CreateEntityRequest body, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var existing = await db.EntityDefinitions.AnyAsync(d => d.TenantId == tenantId && d.EntityName == body.EntityName, ct);
        if (existing) return Results.Conflict($"Entity '{body.EntityName}' already exists.");
        var def = CustomEntityDefinition.Create(tenantId, body.EntityName, body.Description);
        foreach (var f in body.Fields ?? [])
            def.AddField(f.FieldName, f.FieldType, f.IsRequired, f.DefaultValue);
        db.EntityDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/extensibility/entities/{def.Id}", new { def.Id, def.EntityName });
    }

    private static async Task<IResult> GetEntityDefinition(Guid id, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var def = await db.EntityDefinitions.AsNoTracking().Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (def is null) return Results.NotFound();
        return Results.Ok(new { def.Id, def.EntityName, def.Description, def.IsActive,
            Fields = def.Fields.Select(f => new { f.FieldName, Type = f.FieldType.ToString(), f.IsRequired, f.DefaultValue }) });
    }

    private static async Task<IResult> AddField(
        Guid id, [FromBody] AddFieldRequest body, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var def = await db.EntityDefinitions.Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (def is null) return Results.NotFound();
        def.AddField(body.FieldName, body.FieldType, body.IsRequired, body.DefaultValue);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateEntity(Guid id, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var def = await db.EntityDefinitions.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);
        if (def is null) return Results.NotFound();
        def.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListRecords(Guid id, ExtensibilityDbContext db, HttpContext ctx,
        [FromQuery] Guid? ownerEntityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var q = db.EntityRecords.AsNoTracking().Where(r => r.TenantId == tenantId && r.DefinitionId == id);
        if (ownerEntityId.HasValue) q = q.Where(r => r.OwnerEntityId == ownerEntityId);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var records = await q.OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.Id, r.OwnerEntityId, r.Payload, r.CreatedAtUtc, r.UpdatedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(records);
    }

    private static async Task<IResult> CreateRecord(
        Guid id, [FromBody] Dictionary<string, object?> fields, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();
        var defExists = await db.EntityDefinitions.AnyAsync(d => d.Id == id && d.TenantId == tenantId && d.IsActive, ct);
        if (!defExists) return Results.NotFound("Entity definition not found or inactive.");
        fields.TryGetValue("ownerEntityId", out var ownerRaw);
        Guid? ownerEntityId = ownerRaw is System.Text.Json.JsonElement je && je.TryGetGuid(out var g) ? g : null;
        var record = CustomEntityRecord.Create(tenantId, id, ownerEntityId, fields);
        db.EntityRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/extensibility/records/{record.Id}", new { record.Id });
    }

    private static async Task<IResult> GetRecord(Guid id, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var record = await db.EntityRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (record is null) return Results.NotFound();
        return Results.Ok(new { record.Id, record.DefinitionId, record.OwnerEntityId, Fields = record.GetFields(), record.CreatedAtUtc, record.UpdatedAtUtc });
    }

    private static async Task<IResult> UpdateRecord(
        Guid id, [FromBody] Dictionary<string, object?> fields, ExtensibilityDbContext db, HttpContext ctx, CancellationToken ct)
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value ?? string.Empty;
        var record = await db.EntityRecords.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
        if (record is null) return Results.NotFound();
        record.Update(fields);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

internal sealed record CreateEntityRequest(string EntityName, string? Description, List<FieldSpec>? Fields);
internal sealed record FieldSpec(string FieldName, CustomFieldType FieldType, bool IsRequired, string? DefaultValue);
internal sealed record AddFieldRequest(string FieldName, CustomFieldType FieldType, bool IsRequired = false, string? DefaultValue = null);
