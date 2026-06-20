// -----------------------------------------------------------------------
// <copyright file="InterventionEndpoints.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Api.BFF;
using Karamchari.Intelligence.Domain.Interventions;
using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.BFF.Intelligence;

/// <summary>
/// Intervention workflow endpoints — the operational surface of the Intervention Library.
///
/// Routes:
///   GET  /api/v1/interventions                               — list instances for the calling manager
///   GET  /api/v1/interventions/recommendations/{employeeId} — ranked template recommendations
///   POST /api/v1/interventions/recommendations/{id}/accept   — accept recommendation → creates instance
///   POST /api/v1/interventions/recommendations/{id}/decline  — decline with reason
///   POST /api/v1/interventions/recommendations/{id}/defer    — defer with reason
///   POST /api/v1/interventions/{id}/start                    — start instance
///   POST /api/v1/interventions/{id}/complete                 — complete with outcome notes
///   POST /api/v1/interventions/{id}/cancel                   — cancel with reason
/// </summary>
public static class InterventionEndpoints
{
    public static WebApplication MapInterventionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/interventions").RequireAuthorization();

        group.MapGet("", ListInstances)
            .WithName("Interventions.List");

        group.MapGet("/recommendations/{employeeId:guid}", GetRankedRecommendations)
            .WithName("Interventions.Recommendations");

        group.MapPost("/recommendations/{recommendationId:guid}/accept", AcceptRecommendation)
            .WithName("Interventions.Accept");

        group.MapPost("/recommendations/{recommendationId:guid}/decline", DeclineRecommendation)
            .WithName("Interventions.Decline");

        group.MapPost("/recommendations/{recommendationId:guid}/defer", DeferRecommendation)
            .WithName("Interventions.Defer");

        group.MapPost("/{instanceId:guid}/start", StartIntervention)
            .WithName("Interventions.Start");

        group.MapPost("/{instanceId:guid}/complete", CompleteIntervention)
            .WithName("Interventions.Complete");

        group.MapPost("/{instanceId:guid}/cancel", CancelIntervention)
            .WithName("Interventions.Cancel");

        return app;
    }

    // GET /api/v1/interventions?employeeId=&status=
    private static async Task<IResult> ListInstances(
        ClaimsPrincipal user,
        IntelligenceDbContext db,
        Guid? employeeId = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var query = db.InterventionInstances
            .Where(i => i.TenantId == tenantId);

        if (employeeId.HasValue)
            query = query.Where(i => i.EmployeeId == employeeId.Value);

        if (!string.IsNullOrEmpty(status)
            && Enum.TryParse<InterventionInstanceStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(i => i.Status == parsedStatus);
        }

        var instances = await query
            .OrderByDescending(i => i.AssignedAt)
            .Select(i => new
            {
                i.Id,
                i.EmployeeId,
                i.TemplateId,
                i.RecommendationId,
                i.OwnerType,
                i.OwnerId,
                Status = i.Status.ToString(),
                i.AssignedAt,
                i.AcceptedAt,
                i.StartedAt,
                i.CompletedAt,
                i.CancelledAt,
                i.OutcomeNotes
            })
            .Take(200)
            .ToListAsync(ct);

        return Results.Ok(instances);
    }

    // GET /api/v1/interventions/recommendations/{employeeId}?signalType=Burnout
    private static async Task<IResult> GetRankedRecommendations(
        Guid employeeId,
        ClaimsPrincipal user,
        IntelligenceDbContext db,
        TemplateRecommendationService svc,
        string? signalType = null,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        // Default to Burnout signal if not specified; normalize intensity from current score
        var resolvedSignalType = Enum.TryParse<WorkforceSignalType>(signalType, ignoreCase: true, out var parsed)
            ? parsed
            : WorkforceSignalType.OvertimeHours28d;

        // Load current score to normalise intensity — prefer burnout, fall back to attrition
        var burnoutScore = await db.WorkforceBurnoutScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => (decimal?)s.Score)
            .FirstOrDefaultAsync(ct);

        var attritionScore = await db.WorkforceAttritionScores
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId)
            .Select(s => (decimal?)s.Score)
            .FirstOrDefaultAsync(ct);

        // Choose the higher risk signal intensity as the denominator
        var intensity = Math.Max(burnoutScore ?? 0m, attritionScore ?? 0m) / 100m;

        var candidates = await svc.GenerateAsync(tenantId, employeeId, resolvedSignalType, intensity, ct);

        return Results.Ok(new
        {
            EmployeeId = employeeId,
            SignalType = resolvedSignalType.ToString(),
            SignalIntensity = intensity,
            BurnoutScore = burnoutScore,
            AttritionScore = attritionScore,
            Candidates = candidates
        });
    }

    // POST /api/v1/interventions/recommendations/{recommendationId}/accept
    private static async Task<IResult> AcceptRecommendation(
        Guid recommendationId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        AcceptRecommendationRequest? body,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var ownerId = user.GetEmployeeIdString();
        var ownerType = body?.OwnerType is not null
            && Enum.TryParse<InterventionOwnerType>(body.OwnerType, ignoreCase: true, out var parsedOwner)
            ? parsedOwner
            : InterventionOwnerType.Manager;

        try
        {
            var instanceId = await svc.AcceptRecommendationAsync(
                tenantId, recommendationId, ownerType, ownerId, ct);

            return Results.Created($"/api/v1/interventions/{instanceId}", new { InstanceId = instanceId });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    // POST /api/v1/interventions/recommendations/{recommendationId}/decline
    private static async Task<IResult> DeclineRecommendation(
        Guid recommendationId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        DispositionRequest? body,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        try
        {
            await svc.DeclineRecommendationAsync(
                tenantId, recommendationId,
                actorId: user.GetEmployeeIdString(),
                reason: body?.Reason, ct);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    }

    // POST /api/v1/interventions/recommendations/{recommendationId}/defer
    private static async Task<IResult> DeferRecommendation(
        Guid recommendationId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        DispositionRequest? body,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        try
        {
            await svc.DeferRecommendationAsync(
                tenantId, recommendationId,
                actorId: user.GetEmployeeIdString(),
                reason: body?.Reason, ct);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    }

    // POST /api/v1/interventions/{instanceId}/start
    private static async Task<IResult> StartIntervention(
        Guid instanceId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        try
        {
            await svc.StartInterventionAsync(tenantId, instanceId, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    }

    // POST /api/v1/interventions/{instanceId}/complete
    private static async Task<IResult> CompleteIntervention(
        Guid instanceId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        CompleteInterventionRequest? body,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        var resolvedBy = user.GetEmployeeIdString() ?? "unknown";

        try
        {
            await svc.CompleteInterventionAsync(
                tenantId, instanceId,
                outcomeNotes: body?.OutcomeNotes,
                resolvedBy: resolvedBy, ct);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    }

    // POST /api/v1/interventions/{instanceId}/cancel
    private static async Task<IResult> CancelIntervention(
        Guid instanceId,
        ClaimsPrincipal user,
        InterventionWorkflowService svc,
        DispositionRequest? body,
        CancellationToken ct = default)
    {
        var tenantId = user.GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return Results.Unauthorized();

        try
        {
            await svc.CancelInterventionAsync(tenantId, instanceId, reason: body?.Reason, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    }

    // ── Request DTOs ─────────────────────────────────────────────────────────

    private sealed record AcceptRecommendationRequest(string? OwnerType);
    private sealed record DispositionRequest(string? Reason);
    private sealed record CompleteInterventionRequest(string? OutcomeNotes);
}
