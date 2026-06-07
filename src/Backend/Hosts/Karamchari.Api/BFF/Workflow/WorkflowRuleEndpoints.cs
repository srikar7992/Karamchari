using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Core.Multitenancy;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Workflow;

/// <summary>
/// Workflow rule condition management.
/// Conditions are evaluated at routing time; the highest-priority matching definition wins.
/// All conditions on a definition are ANDed (all must hold).
/// </summary>
public static class WorkflowRuleEndpoints
{
    public static WebApplication MapWorkflowRuleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/workflow/definitions").RequireAuthorization();

        group.MapGet("/", ListDefinitions).WithName("Workflow.Definitions.List");
        group.MapGet("/{id:guid}", GetDefinition).WithName("Workflow.Definitions.Get");
        group.MapPost("/{id:guid}/conditions", AddCondition).WithName("Workflow.Definitions.AddCondition");
        group.MapDelete("/{id:guid}/conditions", ClearConditions).WithName("Workflow.Definitions.ClearConditions");

        var routeGroup = app.MapGroup("/api/v1/workflow/route").RequireAuthorization();
        routeGroup.MapPost("/", RouteRequest).WithName("Workflow.Route");

        return app;
    }

    private static async Task<IResult> ListDefinitions(
        [FromQuery] string? entityType,
        [FromQuery] bool activeOnly = true,
        ClaimsPrincipal user = default!,
        WorkflowDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.WorkflowDefinitions.Where(d => d.TenantId == tenantId);
        if (activeOnly) query = query.Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(d => d.EntityType == entityType);

        var defs = await query
            .OrderByDescending(d => d.Priority)
            .ThenBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.EntityType,
                d.Priority,
                d.IsActive,
                d.ConditionsJson,
                StepCount = d.Steps.Count,
            })
            .ToListAsync(ct);

        return Results.Ok(defs);
    }

    private static async Task<IResult> GetDefinition(
        Guid id,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var def = await db.WorkflowDefinitions
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);

        if (def is null) return Results.NotFound();

        return Results.Ok(new
        {
            def.Id,
            def.Name,
            def.EntityType,
            def.Priority,
            def.IsActive,
            Conditions = def.Conditions,
            Steps = def.Steps.OrderBy(s => s.Order).Select(s => new
            {
                s.Id,
                s.Order,
                s.Name,
                s.IsParallel,
                s.QuorumRule,
                s.QuorumThreshold,
                ApproverRoles = s.GetApproverRoles(),
            }),
        });
    }

    private static async Task<IResult> AddCondition(
        Guid id,
        [FromBody] AddConditionRequest req,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var def = await db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);
        if (def is null) return Results.NotFound();

        try
        {
            var condition = new WorkflowCondition(req.Field, req.Operator, req.Value);
            def.AddCondition(condition);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { def.Id, Conditions = def.Conditions });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ClearConditions(
        Guid id,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var def = await db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);
        if (def is null) return Results.NotFound();

        def.ClearConditions();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { def.Id, ConditionCount = 0 });
    }

    /// <summary>
    /// Dry-run routing: given an entity type and a context dictionary, returns which definition
    /// would be selected. Useful for testing rule expressions before deploying.
    /// </summary>
    private static async Task<IResult> RouteRequest(
        [FromBody] RouteRequestBody req,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var candidates = await db.WorkflowDefinitions
            .Include(d => d.Steps)
            .Where(d => d.TenantId == tenantId && d.IsActive && d.EntityType == req.EntityType)
            .ToListAsync(ct);

        var routingReq = new WorkflowRoutingRequest(req.EntityType, tenantId, req.Context);
        var selected = WorkflowRouter.Route(candidates, routingReq);

        if (selected is null)
            return Results.Ok(new { Matched = false, Message = "No active definition matches the provided context." });

        return Results.Ok(new
        {
            Matched = true,
            selected.Id,
            selected.Name,
            selected.Priority,
            Conditions = selected.Conditions,
            StepCount = selected.Steps.Count,
        });
    }

    private sealed record AddConditionRequest(
        string Field,
        ConditionOperator Operator,
        string Value);

    private sealed record RouteRequestBody(
        string EntityType,
        Dictionary<string, object>? Context = null);
}
