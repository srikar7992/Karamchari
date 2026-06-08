using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Core.Multitenancy;
using Karamchari.Workflow.Domain;
using Karamchari.Workflow.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Workflow;

/// <summary>
/// Workflow rule condition management and policy lifecycle.
///
/// Condition model (Phase 13): rules can be composed as nested AND/OR/NOT groups via
/// <c>PUT /api/v1/workflow/definitions/{id}/expression</c>. The legacy flat-list endpoints
/// (AddCondition / ClearConditions) remain for backward compatibility.
///
/// Policy lifecycle: Draft → InReview → Approved → Published → Deprecated.
/// Only Published definitions participate in routing.
/// Effective-date windows further restrict routing to a calendar range.
/// </summary>
public static class WorkflowRuleEndpoints
{
    public static WebApplication MapWorkflowRuleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/workflow/definitions").RequireAuthorization();

        // Query
        group.MapGet("/", ListDefinitions).WithName("Workflow.Definitions.List");
        group.MapGet("/{id:guid}", GetDefinition).WithName("Workflow.Definitions.Get");

        // Legacy flat-condition management (Phase 12 compat)
        group.MapPost("/{id:guid}/conditions", AddCondition).WithName("Workflow.Definitions.AddCondition");
        group.MapDelete("/{id:guid}/conditions", ClearConditions).WithName("Workflow.Definitions.ClearConditions");

        // Phase 13: expression groups (OR/AND/NOT nesting)
        group.MapPut("/{id:guid}/expression", SetExpression).WithName("Workflow.Definitions.SetExpression");

        // Phase 13: effective-date window
        group.MapPut("/{id:guid}/effective-dates", SetEffectiveDates).WithName("Workflow.Definitions.SetEffectiveDates");

        // Phase 13: policy lifecycle
        group.MapPost("/{id:guid}/submit-for-review", SubmitForReview).WithName("Workflow.Definitions.SubmitForReview");
        group.MapPost("/{id:guid}/approve", Approve).WithName("Workflow.Definitions.Approve");
        group.MapPost("/{id:guid}/publish", Publish).WithName("Workflow.Definitions.Publish");
        group.MapPost("/{id:guid}/deprecate", Deprecate).WithName("Workflow.Definitions.Deprecate");
        group.MapPost("/{id:guid}/retract", Retract).WithName("Workflow.Definitions.Retract");

        // Routing dry-run
        var routeGroup = app.MapGroup("/api/v1/workflow/route").RequireAuthorization();
        routeGroup.MapPost("/", RouteRequest).WithName("Workflow.Route");

        return app;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListDefinitions(
        [FromQuery] string? entityType,
        [FromQuery] string? status,
        [FromQuery] bool activeOnly = false,
        ClaimsPrincipal user = default!,
        WorkflowDbContext db = default!,
        CancellationToken ct = default)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var query = db.WorkflowDefinitions.Where(d => d.TenantId == tenantId);
        if (activeOnly) query = query.Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(d => d.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == Enum.Parse<PolicyStatus>(status, true));

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
                Status = d.Status.ToString(),
                d.EffectiveFrom,
                d.EffectiveTo,
                d.ConditionsJson,
                HasExpression = d.ExpressionJson != null,
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
            Status = def.Status.ToString(),
            def.EffectiveFrom,
            def.EffectiveTo,
            Expression = def.Expression,       // fully resolved (OR/AND/NOT tree or legacy flat list)
            LegacyConditions = def.Conditions, // backward compat view
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

    // ── Legacy flat-condition management ─────────────────────────────────────

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

    // ── Expression engine ─────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the routing expression with a composed AND/OR/NOT tree.
    /// Supports arbitrary nesting. Example body:
    /// <code>
    /// {
    ///   "operator": "And",
    ///   "negate": false,
    ///   "conditions": [],
    ///   "groups": [
    ///     { "operator": "Or", "conditions": [
    ///       { "field": "Amount", "operator": "GreaterThan", "value": "10000" },
    ///       { "field": "Department", "operator": "Equals", "value": "Finance" }
    ///     ], "groups": [] },
    ///     { "operator": "And", "negate": true, "conditions": [
    ///       { "field": "Executive", "operator": "Equals", "value": "true" }
    ///     ], "groups": [] }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    private static async Task<IResult> SetExpression(
        Guid id,
        [FromBody] ConditionGroup expression,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var def = await db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);
        if (def is null) return Results.NotFound();

        def.SetExpression(expression);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { def.Id, Expression = def.Expression });
    }

    // ── Effective dates ────────────────────────────────────────────────────────

    private static async Task<IResult> SetEffectiveDates(
        Guid id,
        [FromBody] EffectiveDatesRequest req,
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
            def.SetEffectiveDates(req.EffectiveFrom, req.EffectiveTo);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { def.Id, def.EffectiveFrom, def.EffectiveTo });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ── Policy lifecycle ──────────────────────────────────────────────────────

    private static async Task<IResult> SubmitForReview(
        Guid id, ClaimsPrincipal user, WorkflowDbContext db, CancellationToken ct)
        => await TransitionStatus(id, user, db, (def, actorId) => def.SubmitForReview(actorId), ct);

    private static async Task<IResult> Approve(
        Guid id, ClaimsPrincipal user, WorkflowDbContext db, CancellationToken ct)
        => await TransitionStatus(id, user, db, (def, actorId) => def.Approve(actorId), ct);

    private static async Task<IResult> Publish(
        Guid id, ClaimsPrincipal user, WorkflowDbContext db, CancellationToken ct)
        => await TransitionStatus(id, user, db, (def, actorId) => def.Publish(actorId), ct);

    private static async Task<IResult> Deprecate(
        Guid id, ClaimsPrincipal user, WorkflowDbContext db, CancellationToken ct)
        => await TransitionStatus(id, user, db, (def, actorId) => def.Deprecate(actorId), ct);

    private static async Task<IResult> Retract(
        Guid id, ClaimsPrincipal user, WorkflowDbContext db, CancellationToken ct)
        => await TransitionStatus(id, user, db, (def, actorId) => def.RetractToDraft(actorId), ct);

    private static async Task<IResult> TransitionStatus(
        Guid id,
        ClaimsPrincipal user,
        WorkflowDbContext db,
        Action<WorkflowDefinition, string> transition,
        CancellationToken ct)
    {
        var (tenantId, _) = user.GetTenantAndEmployee();
        if (tenantId is null) return Results.Unauthorized();

        var actorId = user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                   ?? user.FindFirstValue("sub")
                   ?? "unknown";

        var def = await db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, ct);
        if (def is null) return Results.NotFound();

        try
        {
            transition(def, actorId);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { def.Id, Status = def.Status.ToString(), def.IsActive });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ── Routing dry-run ───────────────────────────────────────────────────────

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
            Status = selected.Status.ToString(),
            selected.EffectiveFrom,
            selected.EffectiveTo,
            Expression = selected.Expression,
            StepCount = selected.Steps.Count,
        });
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    private sealed record AddConditionRequest(
        string Field,
        ConditionOperator Operator,
        string Value);

    private sealed record EffectiveDatesRequest(
        DateTime? EffectiveFrom,
        DateTime? EffectiveTo);

    private sealed record RouteRequestBody(
        string EntityType,
        Dictionary<string, object>? Context = null);
}
